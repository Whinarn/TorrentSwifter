#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TorrentSwifter.Logging;
using TorrentSwifter.Network;
using TorrentSwifter.Preferences;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    // TODO: Add support for extensions

    /// <summary>
    /// A torrent peer standard TCP connection.
    /// </summary>
    public sealed class PeerConnectionTCP : PeerConnection
    {
        #region Consts
        private const int ReceiveBufferMaxSize = 128 * 1024; // 128kB
        private const int MaximumAllowedRequestSize = 16 * 1024; // 16kB

        private const string ProtocolName = "BitTorrent protocol";

        private const int KeepAliveInterval = 2 * 60 * 1000; // Every 2 minutes
        #endregion

        #region Enums
        private enum MessageType : int
        {
            Unknown = -3,
            Handshake = -2,
            KeepAlive = -1,
            Choke = 0,
            Unchoke = 1,
            Interested = 2,
            NotInterested = 3,
            Have = 4,
            BitField = 5,
            Request = 6,
            Piece = 7,
            Cancel = 8,
            Port = 9
        }
        #endregion

        #region Fields
        private Socket socket = null;
        private bool isConnecting = false;
        private bool isConnected = false;
        private bool isHandshakeSent = false;
        private bool isBitFieldSent = false;
        private bool isHandshakeReceived = false;
        private bool isChokedByUs = true;
        private bool isInterestedByUs = false;
        private bool isChokedByRemote = true;
        private bool isInterestedByRemote = false;

        private InfoHash infoHash = default(InfoHash);
        private PeerID peerID = default(PeerID);

        private int receiveOffset = 0;
        private byte[] receiveBuffer = new byte[ReceiveBufferMaxSize];
        private Packet receivedPacket = null;

        private DateTime sentHandshakeTime = DateTime.UtcNow;
        private DateTime lastActiveTime = DateTime.UtcNow;
        private DateTime lastKeepAliveTime = DateTime.UtcNow;

        private SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        #endregion

        #region Properties
        /// <summary>
        /// Gets if we are currently attempting to connect.
        /// </summary>
        public override bool IsConnecting
        {
            get { return isConnecting; }
        }

        /// <summary>
        /// Gets if this connection has been established.
        /// </summary>
        public override bool IsConnected
        {
            get { return isConnected; }
        }

        /// <summary>
        /// Gets if this connection has handshaked successfully.
        /// </summary>
        public override bool IsHandshaked
        {
            get { return isHandshakeReceived; }
        }

        /// <summary>
        /// Gets the ID of the peer we are connected to.
        /// </summary>
        public override PeerID PeerID
        {
            get { return peerID; }
        }

        /// <summary>
        /// Gets if we are currently choking the peer.
        /// </summary>
        public override bool IsChokedByUs
        {
            get { return isChokedByUs; }
        }

        /// <summary>
        /// Gets if we are interested in this peer.
        /// </summary>
        public override bool IsInterestedByUs
        {
            get { return isInterestedByUs; }
        }

        /// <summary>
        /// Gets if we are currently choked by the remote.
        /// </summary>
        public override bool IsChokedByRemote
        {
            get { return isChokedByRemote; }
        }

        /// <summary>
        /// Gets if this peer is interested in some of our pieces.
        /// </summary>
        public override bool IsInterestedByRemote
        {
            get { return isInterestedByRemote; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new TCP peer connection.
        /// </summary>
        /// <param name="torrent">The parent torrent.</param>
        /// <param name="peer">The parent peer.</param>
        /// <param name="endPoint">The peer end-point.</param>
        public PeerConnectionTCP(Torrent torrent, Peer peer, EndPoint endPoint)
            : base(torrent, peer, endPoint)
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            Initialize();
        }

        /// <summary>
        /// Creates a new TCP peer connection.
        /// </summary>
        /// <param name="socket">The socket.</param>
        internal PeerConnectionTCP(Socket socket)
            : base(socket.RemoteEndPoint)
        {
            if (socket == null)
                throw new ArgumentNullException("socket");

            this.socket = socket;
            Initialize();

            isConnected = true;
            isConnecting = false;

            OnConnected();
            StartDataReceive(true);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Connects to this peer synchronously.
        /// </summary>
        public override void Connect()
        {
            if (isConnecting || isConnected)
                return;

            isConnecting = true;
            isConnected = false;
            try
            {
                socket.Connect(endPoint);
                isConnected = true;
                OnConnected();
                StartDataReceive(true);
            }
            catch (SocketException ex)
            {
                var failedReason = GetConnectionFailedReason(ex.SocketErrorCode);
                OnConnectionFailed(failedReason);
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
                OnConnectionFailed(ConnectionFailedReason.Unknown);
            }
            finally
            {
                isConnecting = false;
            }
        }

        /// <summary>
        /// Connects to this peer asynchronously.
        /// </summary>
        /// <returns>The connect asynchronous task.</returns>
        public override async Task ConnectAsync()
        {
            if (isConnecting || isConnected)
                return;

            isConnecting = true;
            isConnected = false;
            try
            {
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endPoint, socket);
                isConnected = true;
                OnConnected();
                StartDataReceive(true);
            }
            catch (SocketException ex)
            {
                var failedReason = GetConnectionFailedReason(ex.SocketErrorCode);
                OnConnectionFailed(failedReason);
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
                OnConnectionFailed(ConnectionFailedReason.Unknown);
            }
            finally
            {
                isConnecting = false;
            }
        }

        /// <summary>
        /// Disconnects from this peer.
        /// </summary>
        public override void Disconnect()
        {
            if (!isConnecting && !isConnected)
                return;

            isConnecting = false;
            isConnected = false;

            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                socket.Close();
                socket = null;
            }

            OnDisconnected();
        }

        /// <summary>
        /// Updates this peer connection.
        /// </summary>
        public override void Update()
        {
            if (!isConnected)
                return;

            if (!isHandshakeReceived)
            {
                // Make sure we get a handshake within a time-frame
                int handshakeTimeout = Prefs.Peer.HandshakeTimeout;
                if (handshakeTimeout > 0)
                {
                    var timeSinceHandshakeSent = DateTime.UtcNow.Subtract(sentHandshakeTime);
                    if (timeSinceHandshakeSent.TotalMilliseconds >= handshakeTimeout)
                    {
                        Disconnect();
                    }
                }
                return;
            }

            // Make sure that the peer doesn't get inactive
            int inactiveTimeout = Prefs.Peer.InactiveTimeout;
            if (inactiveTimeout > 0)
            {
                var timeSinceActive = DateTime.UtcNow.Subtract(lastActiveTime);
                if (timeSinceActive.TotalMilliseconds >= inactiveTimeout)
                {
                    Disconnect();
                    return;
                }
            }

            var timeSinceLastKeepAlive = DateTime.UtcNow.Subtract(lastKeepAliveTime);
            if (timeSinceLastKeepAlive.TotalMilliseconds > KeepAliveInterval)
            {
                SendKeepAlive();
            }
        }

        /// <summary>
        /// Sends the interest state to this peer.
        /// </summary>
        /// <param name="state">If interested (true), or not (false).</param>
        public override void SendInterested(bool state)
        {
            if (!isConnected || !isBitFieldSent)
                return;
            else if (state == isInterestedByUs)
                return;

            if (state)
                SendInterested();
            else
                SendNotInterested();
        }

        /// <summary>
        /// Sends the choked state to this peer.
        /// </summary>
        /// <param name="state">If choked (true), or not (false).</param>
        public override void SendChoked(bool state)
        {
            if (!isConnected || !isBitFieldSent)
                return;
            else if (state == isChokedByUs)
                return;

            if (state)
                SendChoke();
            else
                SendUnchoke();
        }

        /// <summary>
        /// Reports that we have a new piece to this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        public override void ReportHavePiece(int pieceIndex)
        {
            if (!isConnected || !isBitFieldSent)
                return;

            SendHave(pieceIndex);
        }

        /// <summary>
        /// Requests a piece of data from this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        /// <returns>The request task, with a result if the request was sent.</returns>
        public override async Task<bool> RequestPieceData(int pieceIndex, int blockIndex)
        {
            if (!isConnected || !isHandshakeReceived)
                return false;

            return await SendRequest(pieceIndex, blockIndex);
        }

        /// <summary>
        /// Cancels a pending request for a piece of data from this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="blockIndex">The block index.</param>
        public override void CancelPieceDataRequest(int pieceIndex, int blockIndex)
        {
            if (!isConnected || !isHandshakeReceived)
                return;

            SendCancel(pieceIndex, blockIndex);
        }

        /// <summary>
        /// Sends a piece of data to this peer.
        /// </summary>
        /// <param name="pieceIndex">The piece index.</param>
        /// <param name="begin">The byte offset within the piece.</param>
        /// <param name="data">The byte data.</param>
        /// <returns>The send task.</returns>
        public override async Task SendPieceData(int pieceIndex, int begin, byte[] data)
        {
            if (!isConnected || !isHandshakeReceived)
                return;

            await SendPiece(pieceIndex, begin, data);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Disposes of this peer connection.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected override void Dispose(bool disposing)
        {
            Disconnect();
        }

        /// <summary>
        /// The peer connection has been successfully established.
        /// </summary>
        protected override void OnConnected()
        {
            isHandshakeReceived = false;
            isChokedByUs = true;
            isInterestedByUs = false;
            isChokedByRemote = true;
            isInterestedByRemote = false;
            isHandshakeSent = false;
            isBitFieldSent = false;

            lastActiveTime = DateTime.UtcNow;
            lastKeepAliveTime = DateTime.UtcNow;

            base.OnConnected();

            SendHandshake();
            SendBitField();
        }

        /// <summary>
        /// The peer connection has been disconnected.
        /// </summary>
        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            isHandshakeReceived = false;
            isHandshakeSent = false;
            isBitFieldSent = false;
        }
        #endregion

        #region Private Methods
        #region Initialize
        private void Initialize()
        {
            socket.LingerState = new LingerOption(true, 10);

            receivedPacket = new Packet(receiveBuffer, 0);
        }
        #endregion

        #region Data Receiving
        private void StartDataReceive(bool firstTime)
        {
            if (socket == null)
                return;

            if (firstTime)
            {
                receiveOffset = 0;
            }
            else if (receiveOffset >= receiveBuffer.Length)
            {
                // There is nothing more to receive in the buffer, and to prevent getting stuck, we simply reset the receive offset
                // This should never happen though...
                receiveOffset = 0;
            }

            try
            {
                socket?.BeginReceive(receiveBuffer, receiveOffset, receiveBuffer.Length - receiveOffset, SocketFlags.None, OnDataReceived, socket);
            }
            catch (ObjectDisposedException)
            {
                // NOTE: If the socket as been disposed of, then we can safely discard this exception and just stop listen
            }
            catch (Exception ex)
            {
                Log.LogDebugException(ex);
                Disconnect();
            }
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            Socket socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                int receivedByteCount = socket.EndReceive(ar);
                if (receivedByteCount <= 0)
                {
                    // The peer has gracefully disconnected
                    Disconnect();
                    return;
                }

                receiveOffset += receivedByteCount;
                Stats.IncreaseDownloadedBytes(receivedByteCount);

                int packetLength = GetPacketLength();
                while (packetLength != -1 && receiveOffset >= packetLength)
                {
                    try
                    {
                        // Handle the packet
                        receivedPacket.Offset = 0;
                        receivedPacket.Length = packetLength;
                        bool handledOK = HandlePacket(receivedPacket);
                        if (!handledOK)
                        {
                            Disconnect();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError("[Peer][{0}] Exception occured when handling a packet: {1}", endPoint, ex.Message);
                        Log.LogErrorException(ex);
                        Disconnect();
                        break;
                    }

                    int remainingLength = (receiveOffset - packetLength);
                    if (remainingLength > 0)
                    {
                        // Copy remaining bytes in the buffer to the start of the buffer
                        Buffer.BlockCopy(receiveBuffer, packetLength, receiveBuffer, 0, remainingLength);
                        receiveOffset = remainingLength;
                    }
                    else
                    {
                        receiveOffset = 0;
                    }
                    packetLength = GetPacketLength();
                }

                if (packetLength > ReceiveBufferMaxSize)
                {
                    Log.LogError("[Peer][{0}] A peer sent a packet that is too large to handle ({1} > {2}). Exploiter?", endPoint, packetLength, ReceiveBufferMaxSize);
                    Disconnect();
                }
            }
            catch (ObjectDisposedException)
            {
                // Safe to consume because this means that we have disconnected
            }
            catch (SocketException ex)
            {
                Log.LogDebugException(ex);
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.LogErrorException(ex);
                Disconnect();
            }
            finally
            {
                StartDataReceive(false);
            }
        }
        #endregion

        #region Data Sending
        private void SendPacket(Packet packet)
        {
            if (socket == null || !isConnected)
                return;

            try
            {
                var packetData = packet.Data;
                int packetLength = packet.Length;

#if DEBUG
                if (isHandshakeSent)
                {
                    packet.Offset = 0;
                    int encodedLength = packet.ReadInt32();
                    if ((encodedLength + 4) != packetLength)
                        throw new InvalidOperationException(string.Format("Attempted to send a packet with size {0} that was expected to be {1}.", packetLength, (encodedLength + 4)));
                }
                else
                {
                    if (packetLength != 68)
                        throw new InvalidOperationException(string.Format("Attempted to send a packet with size {0} that was expected to be 64.", packetLength));
                }
#endif

                sendSemaphore.Wait();
                try
                {
                    socket.BeginSend(packetData, 0, packetLength, SocketFlags.None, OnDataSent, socket);
                }
                finally
                {
                    sendSemaphore.Release();
                }
            }
            catch (ObjectDisposedException)
            {
                // Safe to consume because this means that we have disconnected
            }
            catch (Exception ex)
            {
                Log.LogDebugException(ex);
                Disconnect();
            }
        }

        private async Task SendPacketAsync(Packet packet)
        {
            if (socket == null || !isConnected)
                return;

            try
            {
                var packetData = packet.Data;
                int packetLength = packet.Length;

#if DEBUG
                if (isHandshakeSent)
                {
                    packet.Offset = 0;
                    int encodedLength = packet.ReadInt32();
                    if ((encodedLength + 4) != packetLength)
                        throw new InvalidOperationException(string.Format("Attempted to send a packet with size {0} that was expected to be {1}.", packetLength, (encodedLength + 4)));
                }
                else
                {
                    if (packetLength != 68)
                        throw new InvalidOperationException(string.Format("Attempted to send a packet with size {0} that was expected to be 64.", packetLength));
                }
#endif

                await sendSemaphore.WaitAsync();
                try
                {
                    var result = socket.BeginSend(packetData, 0, packetLength, SocketFlags.None, null, socket);
                    int sentByteCount = await Task.Factory.FromAsync(result, socket.EndSend);
                    Stats.IncreaseUploadedBytes(sentByteCount);
                }
                finally
                {
                    sendSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Log.LogDebugException(ex);
                Disconnect();
            }
        }

        private void OnDataSent(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            if (socket == null)
                return;

            try
            {
                int sentByteCount = socket.EndSend(ar);
                Stats.IncreaseUploadedBytes(sentByteCount);
            }
            catch (ObjectDisposedException)
            {
                // We can safely consume this, since we are already disconnected
            }
            catch (Exception ex)
            {
                Log.LogDebugException(ex);
                Disconnect();
            }
        }
        #endregion

        #region Sending Packets
        private void SendHandshake()
        {
            // Ignore if we have already sent the handshake or if we don't have torrent information yet
            if (isHandshakeSent || torrent == null)
                return;

            var infoHash = torrent.InfoHash;
            var peerID = torrent.PeerID;

            var packet = new Packet(68);
            packet.WriteByte((byte)ProtocolName.Length);
            packet.WriteString(ProtocolName);
            packet.WriteInt64(0); // flags
            packet.Write(infoHash.Hash, 0, 20);
            packet.Write(peerID.ID, 0, 20);
            SendPacket(packet);

            isHandshakeSent = true;
            sentHandshakeTime = DateTime.UtcNow;
        }

        private void SendKeepAlive()
        {
            var packet = new Packet(4);
            packet.WriteInt32(0);
            SendPacket(packet);

            lastKeepAliveTime = DateTime.UtcNow;
        }

        private void SendChoke()
        {
            if (isChokedByUs)
                return;

            var packet = CreatePacket(MessageType.Choke, 0);
            SendPacket(packet);
            isChokedByUs = true;
        }

        private void SendUnchoke()
        {
            if (!isChokedByUs)
                return;

            var packet = CreatePacket(MessageType.Unchoke, 0);
            SendPacket(packet);
            isChokedByUs = false;
        }

        private void SendInterested()
        {
            if (isInterestedByUs)
                return;

            var packet = CreatePacket(MessageType.Interested, 0);
            SendPacket(packet);
            isInterestedByUs = true;
        }

        private void SendNotInterested()
        {
            if (!isInterestedByUs)
                return;

            var packet = CreatePacket(MessageType.NotInterested, 0);
            SendPacket(packet);
            isInterestedByUs = false;
        }

        private void SendHave(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount)
                throw new ArgumentOutOfRangeException("pieceIndex");

            var packet = CreatePacket(MessageType.Have, 4);
            packet.WriteInt32(pieceIndex);
            SendPacket(packet);
        }

        private void SendBitField()
        {
            // Ignore if we have already sent the bit field or if we haven't sent the handshake yet
            if (isBitFieldSent || !isHandshakeSent)
                return;

            if (torrent.Mode.MaskBitmasks)
            {
                var bitField = torrent.BitField;
                int bitFieldByteCount = bitField.ByteLength;
                var packet = CreatePacket(MessageType.BitField, bitFieldByteCount);
                packet.WriteZeroes(bitFieldByteCount);
                SendPacket(packet);
            }
            else
            {
                var bitField = torrent.BitField;
                var bitFieldBytes = bitField.Buffer;
                int bitFieldByteCount = bitField.ByteLength;
                var packet = CreatePacket(MessageType.BitField, bitFieldByteCount);
                packet.Write(bitFieldBytes, 0, bitFieldByteCount);
                SendPacket(packet);
            }

            isBitFieldSent = true;
        }

        private async Task<bool> SendRequest(int pieceIndex, int blockIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount)
                throw new ArgumentOutOfRangeException("pieceIndex");
            else if (blockIndex < 0)
                throw new ArgumentOutOfRangeException("blockIndex");

            var piece = torrent.GetPiece(pieceIndex);
            if (blockIndex >= piece.BlockCount)
                throw new ArgumentOutOfRangeException("blockIndex");

            // Prevent sending requests if we have been choked by the remote
            if (isChokedByRemote)
                return false;

            var block = piece.GetBlock(blockIndex);
            int begin = blockIndex * torrent.BlockSize;
            int length = block.Size;

            var packet = CreatePacket(MessageType.Request, 12);
            packet.WriteInt32(pieceIndex);
            packet.WriteInt32(begin);
            packet.WriteInt32(length);
            await SendPacketAsync(packet);
            return true;
        }

        private async Task SendPiece(int pieceIndex, int begin, byte[] data)
        {
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount)
                throw new ArgumentOutOfRangeException("pieceIndex");
            else if (begin < 0)
                throw new ArgumentOutOfRangeException("begin");
            else if (data == null)
                throw new ArgumentNullException("data");

            var packet = CreatePacket(MessageType.Piece, 8 + data.Length);
            packet.WriteInt32(pieceIndex);
            packet.WriteInt32(begin);
            packet.Write(data, 0, data.Length);
            await SendPacketAsync(packet);

            torrent.IncreaseSessionUploadedBytes(data.Length);
        }

        private void SendCancel(int pieceIndex, int blockIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount)
                throw new ArgumentOutOfRangeException("pieceIndex");
            else if (blockIndex < 0)
                throw new ArgumentOutOfRangeException("blockIndex");

            var piece = torrent.GetPiece(pieceIndex);
            if (blockIndex >= piece.BlockCount)
                throw new ArgumentOutOfRangeException("blockIndex");

            var block = piece.GetBlock(blockIndex);
            int begin = blockIndex * torrent.BlockSize;
            int length = block.Size;

            var packet = CreatePacket(MessageType.Cancel, 12);
            packet.WriteInt32(pieceIndex);
            packet.WriteInt32(begin);
            packet.WriteInt32(length);
            SendPacket(packet);
        }

        private Packet CreatePacket(MessageType messageType, int length)
        {
            if (messageType < 0)
                throw new ArgumentException("The message type is invalid.", "messageType");
            else if (length < 0)
                throw new ArgumentOutOfRangeException("The length cannot be negative.", "length");

            var packet = new Packet(5 + length);
            packet.WriteInt32(1 + length);
            packet.WriteByte((byte)messageType);
            return packet;
        }
        #endregion

        #region Handle Packets
        private bool HandlePacket(Packet packet)
        {
            lastActiveTime = DateTime.UtcNow;

            var messageType = GetMessageType(packet);
            if (messageType == MessageType.Unknown)
            {
                Log.LogWarning("[Peer][{0}] Received unhandled message of {1} bytes.", endPoint, packet.Length);
                return true;
            }

            switch (messageType)
            {
                case MessageType.Handshake:
                    return HandleHandshake(packet);
                case MessageType.KeepAlive:
                    return HandleKeepAlive(packet);
                case MessageType.Choke:
                    return HandleChoke(packet);
                case MessageType.Unchoke:
                    return HandleUnchoke(packet);
                case MessageType.Interested:
                    return HandleInterested(packet);
                case MessageType.NotInterested:
                    return HandleNotInterested(packet);
                case MessageType.Have:
                    return HandleHave(packet);
                case MessageType.BitField:
                    return HandleBitField(packet);
                case MessageType.Request:
                    return HandleRequest(packet);
                case MessageType.Piece:
                    return HandlePiece(packet);
                case MessageType.Cancel:
                    return HandleCancel(packet);
                case MessageType.Port:
                    return HandlePort(packet);
                default:
                    Log.LogError("[Peer][{0}] Received unhandled message type: {1}", endPoint, messageType);
                    return false;
            }
        }

        private bool HandleHandshake(Packet packet)
        {
            if (packet.Length != 68)
            {
                Log.LogWarning("[Peer][{0}] Invalid handshake received with {1} bytes (should have been 68).", endPoint, packet.Length);
                return false;
            }

            int protocolNameLength = packet.ReadByte();
            if (packet.RemainingBytes < (protocolNameLength + 40))
            {
                Log.LogWarning("[Peer][{0}] Invalid handshake received with {1} bytes (protocol name length: {2}).", endPoint, packet.Length, protocolNameLength);
                return false;
            }

            string protocolName = packet.ReadString(protocolNameLength);
            if (!string.Equals(protocolName, ProtocolName))
            {
                Log.LogWarning("[Peer][{0}] Handshake protocol is not supported: {1}", endPoint, protocolName);
                return false;
            }

            // Skip 8 bytes of flags
            packet.Skip(8);

            byte[] hashBytes = packet.ReadBytes(20);
            byte[] idBytes = packet.ReadBytes(20);

            var infoHash = new InfoHash(hashBytes);
            var peerID = new PeerID(idBytes);

            if (torrent != null && !torrent.InfoHash.Equals(infoHash))
            {
                Log.LogWarning("[Peer][{0}] Handshake with invalid info hash: {1}", endPoint, infoHash);
                return false;
            }
            else if (torrent != null && peerID.Equals(torrent.PeerID))
            {
                peer.IsSelf = true;
                Log.LogDebug("[Peer][{0}] Handshake with ourself. Closing connection.", endPoint);
                return false;
            }
            else if (torrent == null)
            {
                var foundTorrent = TorrentRegistry.FindTorrentByInfoHash(infoHash);
                if (foundTorrent == null)
                {
                    Log.LogWarning("[Peer][{0}] Handshake with unknown info hash: {1}", endPoint, infoHash);
                    return false;
                }
                else if (foundTorrent.IsStoppedOrStopping)
                {
                    Log.LogWarning("[Peer][{0}] Handshake received for a torrent that is stopped or currently stopping: {1}", endPoint, infoHash);
                    return false;
                }

                torrent = foundTorrent;
                if (peerID.Equals(torrent.PeerID))
                {
                    Log.LogDebug("[Peer][{0}] Handshake with ourself. Closing connection.", endPoint);
                    return false;
                }

                peer = torrent.OnPeerHandshaked(peerID, this);
            }

            if (peer.ID.IsNone)
            {
                // If the peer had no ID prior, then we set it now and register the ID with the torrent
                peer.ID = peerID;
                torrent.RegisterPeerWithID(peerID, peer);
            }
            else if (!peerID.Equals(peer.ID))
            {
                Log.LogWarning("[Peer][{0}] Handshake with invalid peer ID: {1}   !==   {2}", endPoint, peerID, peer.ID);
                return false;
            }

            this.infoHash = infoHash;
            this.peerID = peerID;
            isHandshakeReceived = true;

            Log.LogDebug("[Peer][{0}] A peer handshaked with us with info hash [{1}] and peer ID [{2}].", endPoint, infoHash, peerID);

            SendHandshake();
            SendBitField();
            OnHandshaked();
            return true;
        }

        private bool HandleKeepAlive(Packet packet)
        {
            Log.LogDebug("[Peer][{0}] A peer asked to keep the connection alive.", endPoint);
            return true;
        }

        private bool HandleChoke(Packet packet)
        {
            if (packet.Length != 5)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'choke' received with {1} bytes (should have been 5).", endPoint, packet.Length);
                return false;
            }
            else if (isChokedByRemote)
            {
                Log.LogDebug("[Peer][{0}] A 'choke' was received while already being choked.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer choked us.", endPoint);
            isChokedByRemote = true;
            OnStateChanged();
            return true;
        }

        private bool HandleUnchoke(Packet packet)
        {
            if (packet.Length != 5)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'unchoke' received with {1} bytes (should have been 5).", endPoint, packet.Length);
                return false;
            }
            else if (!isChokedByRemote)
            {
                Log.LogDebug("[Peer][{0}] A 'unchoke' was received while already not being choked.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer unchoked us.", endPoint);
            isChokedByRemote = false;
            OnStateChanged();
            return true;
        }

        private bool HandleInterested(Packet packet)
        {
            if (packet.Length != 5)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'interested' received with {1} bytes (should have been 5).", endPoint, packet.Length);
                return false;
            }
            else if (isInterestedByRemote)
            {
                Log.LogDebug("[Peer][{0}] An 'interested' was received while already being interested.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer is interested.", endPoint);
            isInterestedByRemote = true;
            OnStateChanged();
            return true;
        }

        private bool HandleNotInterested(Packet packet)
        {
            if (packet.Length != 5)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'not interested' received with {1} bytes (should have been 5).", endPoint, packet.Length);
                return false;
            }
            else if (!isInterestedByRemote)
            {
                Log.LogDebug("[Peer][{0}] A 'not interested' was received while already not being interested.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer is no longer interested.", endPoint);
            isInterestedByRemote = false;
            OnStateChanged();
            return true;
        }

        private bool HandleHave(Packet packet)
        {
            if (packet.Length != 9)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'have' received with {1} bytes (should have been 9).", endPoint, packet.Length);
                return false;
            }

            int pieceIndex = packet.ReadInt32();
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceSize)
            {
                Log.LogWarning("[Peer][{0}] A peer sent 'have' with invalid arguments. Index: {1}", endPoint, pieceIndex);
                return false;
            }

            Log.LogDebug("[Peer][{0}] Peer is reporting that it has piece {1}", endPoint, pieceIndex);

            OnHavePiece(pieceIndex);
            return true;
        }

        private bool HandleBitField(Packet packet)
        {
            int pieceCount = torrent.PieceCount;
            int bitFieldByteCount = ((pieceCount + 7) >> 3);
            int expectedLength = (5 + bitFieldByteCount);
            if (packet.Length != expectedLength)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'bit field' received with {1} bytes (should have been {2}).", endPoint, packet.Length, expectedLength);
                return false;
            }

            Log.LogDebug("[Peer][{0}] The peer sent their bit field of {1} bytes.", endPoint, bitFieldByteCount);

            byte[] bitFieldBytes = packet.ReadBytes(bitFieldByteCount);
            var bitField = new BitField(bitFieldBytes, pieceCount);
            OnBitFieldReceived(bitField);
            return true;
        }

        private bool HandleRequest(Packet packet)
        {
            if (packet.Length != 17)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'request' received with {1} bytes (should have been 17).", endPoint, packet.Length);
                return false;
            }
            else if (isChokedByUs)
            {
                // TODO: Add penalty points for bad behaviour
                Log.LogWarning("[Peer][{0}] A peer sent a request while being choked.", endPoint);
                return true;
            }

            int pieceIndex = packet.ReadInt32();
            int begin = packet.ReadInt32();
            int length = packet.ReadInt32();

            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount || begin < 0 || length <= 0)
            {
                Log.LogWarning("[Peer][{0}] A peer sent a request with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }
            else if (length > MaximumAllowedRequestSize)
            {
                Log.LogWarning("[Peer][{0}] A peer requested more data than allowed. {1} bytes was requested.", endPoint, length);
                return false;
            }

            var piece = torrent.GetPiece(pieceIndex);
            if (begin >= piece.Size || (begin + length) > piece.Size)
            {
                Log.LogWarning("[Peer][{0}] A peer sent a request with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }
            else if (!piece.IsVerified)
            {
                Log.LogWarning("[Peer][{0}] A peer requested data from a piece that is not verified. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }

            Log.LogDebug("[Peer][{0}] A peer sent a request to us. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
            torrent.OnPieceBlockRequested(peer, pieceIndex, begin, length);
            return true;
        }

        private bool HandlePiece(Packet packet)
        {
            int pieceIndex = packet.ReadInt32();
            int begin = packet.ReadInt32();
            int length = (packet.Length - packet.Offset);

            // The piece index must be valid, begin cannot be negative and must be modular to the block-size
            int blockSize = torrent.BlockSize;
            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount || begin < 0 || (begin % blockSize) != 0)
            {
                Log.LogWarning("[Peer][{0}] A peer sent piece data with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }

            int blockIndex = (begin / blockSize);
            var piece = torrent.GetPiece(pieceIndex);
            if (begin >= piece.Size || (begin + length) > piece.Size || blockIndex >= piece.BlockCount)
            {
                Log.LogWarning("[Peer][{0}] A peer sent piece data with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }

            var block = piece.GetBlock(blockIndex);
            if (length != block.Size)
            {
                Log.LogWarning("[Peer][{0}] A peer sent piece data with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }
            else if (!block.IsRequested)
            {
                // TODO: Should we be more offended with this and disconnect the peer? Or do we simply write it up as bad behaviour and kick them out
                //       once they have commited enough offences?
                Log.LogDebug("[Peer][{0}] A peer sent piece data that was not requested. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return true;
            }

            Log.LogDebug("[Peer][{0}] A peer sent piece data. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);

            byte[] data = packet.ReadBytes(length);
            torrent.IncreaseSessionDownloadedBytes(length);
            torrent.OnReceivedPieceBlock(peer, pieceIndex, blockIndex, data);
            return true;
        }

        private bool HandleCancel(Packet packet)
        {
            if (packet.Length != 17)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'cancel' received with {1} bytes (should have been 17).", endPoint, packet.Length);
                return false;
            }

            int pieceIndex = packet.ReadInt32();
            int begin = packet.ReadInt32();
            int length = packet.ReadInt32();

            if (pieceIndex < 0 || pieceIndex >= torrent.PieceCount || begin < 0 || length < 0)
            {
                Log.LogWarning("[Peer][{0}] A peer cancelled a request with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
                return false;
            }

            Log.LogDebug("[Peer][{0}] A peer cancelled request to us. Index: {1}, Begin: {2}, Length: {3}", endPoint, pieceIndex, begin, length);
            torrent.OnPieceBlockCancelled(peer, pieceIndex, begin, length);
            return true;
        }

        private bool HandlePort(Packet packet)
        {
            if (packet.Length != 7)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'port' received with {1} bytes (should have been 7).", endPoint, packet.Length);
                return false;
            }

            int port = packet.ReadUInt16();
            Log.LogDebug("[Peer][{0}] A peer sent us a port to its DHT node: {1}", endPoint, port);
            return true;
        }
        #endregion

        #region Packet/Message Methods
        private int GetPacketLength()
        {
            if (!isHandshakeReceived)
                return 68;
            else if (receiveOffset < 4)
                return -1;

            int packetLength = ((receiveBuffer[0] << 24) | (receiveBuffer[1] << 16) | (receiveBuffer[2] << 8) | receiveBuffer[3]);
            packetLength += 4;
            return packetLength;
        }

        private MessageType GetMessageType(Packet packet)
        {
            if (!isHandshakeReceived)
                return MessageType.Handshake;
            else if (packet.Length == 4 && packet.ReadInt32() == 0)
                return MessageType.KeepAlive;
            else if (packet.Length <= 4)
                return MessageType.Unknown;

            packet.Offset = 4;
            int messageTypeInt = packet.ReadByte();
            if (Enum.IsDefined(typeof(MessageType), messageTypeInt))
            {
                return (MessageType)Enum.ToObject(typeof(MessageType), messageTypeInt);
            }
            else
            {
                return MessageType.Unknown;
            }
        }
        #endregion

        #region Helper Methods
        private static ConnectionFailedReason GetConnectionFailedReason(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.ConnectionRefused:
                    return ConnectionFailedReason.Refused;
                case SocketError.ConnectionAborted:
                case SocketError.OperationAborted:
                    return ConnectionFailedReason.Aborted;
                case SocketError.TimedOut:
                    return ConnectionFailedReason.TimedOut;
                case SocketError.HostDown:
                    return ConnectionFailedReason.HostDown;
                case SocketError.HostUnreachable:
                    return ConnectionFailedReason.HostUnreachable;
                case SocketError.TryAgain:
                case SocketError.HostNotFound:
                    return ConnectionFailedReason.NameResolve;
                case SocketError.NetworkDown:
                case SocketError.NetworkReset:
                case SocketError.NetworkUnreachable:
                    return ConnectionFailedReason.NoInternetConnection;
                case SocketError.AddressFamilyNotSupported:
                case SocketError.OperationNotSupported:
                case SocketError.ProtocolFamilyNotSupported:
                case SocketError.ProtocolNotSupported:
                case SocketError.SocketNotSupported:
                case SocketError.VersionNotSupported:
                    return ConnectionFailedReason.NotSupported;
                case SocketError.AccessDenied:
                    return ConnectionFailedReason.AccessDenied;
                default:
                    return ConnectionFailedReason.Unknown;
            }
        }
        #endregion
        #endregion
    }
}
