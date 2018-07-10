using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TorrentSwifter.Logging;
using TorrentSwifter.Network;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer standard TCP connection.
    /// </summary>
    public sealed class PeerConnectionTCP : PeerConnection
    {
        #region Consts
        private const int ReceiveBufferMaxSize = 128 * 1024; // 128kB
        private const int MaximumAllowedRequestSize = 16 * 1024; // 16kB

        private const string ProtocolName = "BitTorrent protocol";
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
            Cancel = 8
        }
        #endregion

        #region Fields
        private Socket socket = null;
        private bool isConnecting = false;
        private bool isConnected = false;
        private bool isHandshakeSent = false;
        private bool isBitFieldSent = false;
        private bool isHandshakeReceived = false;
        private bool isChoked = true;
        private bool isInterested = false;

        private InfoHash infoHash = default(InfoHash);
        private PeerID peerID = default(PeerID);

        private int receiveOffset = 0;
        private byte[] receiveBuffer = new byte[ReceiveBufferMaxSize];
        private Packet receivedPacket = null;

        private object sendSyncObj = new object();
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
        /// Gets if the handshake has been received from the remote.
        /// </summary>
        public bool IsHandshakeReceived
        {
            get { return isHandshakeReceived; }
        }

        /// <summary>
        /// Gets if this peer connection is currently choked.
        /// </summary>
        public bool IsChoked
        {
            get { return isChoked; }
        }

        /// <summary>
        /// Gets if this peer is interested in some of our pieces.
        /// </summary>
        public bool IsInterested
        {
            get { return isInterested; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new TCP peer connection.
        /// </summary>
        /// <param name="torrent">The parent torrent.</param>
        /// <param name="endPoint">The peer end-point.</param>
        public PeerConnectionTCP(Torrent torrent, EndPoint endPoint)
            : base(torrent, endPoint)
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
                socket.Shutdown(SocketShutdown.Both);
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
            // TODO: Disconnect automatically after 1 minute (or something?) of not receiving handshake from the remote
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
            isChoked = true;
            isInterested = false;
            isHandshakeSent = false;
            isBitFieldSent = false;

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
            socket.DualMode = true;
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

                    // Copy remaining bytes in the buffer to the start of the buffer
                    Buffer.BlockCopy(receiveBuffer, packetLength, receiveBuffer, 0, receiveOffset - packetLength);
                    receiveOffset = 0;
                    packetLength = GetPacketLength();
                }

                if (packetLength > ReceiveBufferMaxSize)
                {
                    Log.LogError("[Peer][{0}] A peer sent a packet that is too large to handle ({1} > {2}). Exploiter?", endPoint, packetLength, ReceiveBufferMaxSize);
                    Disconnect();
                }
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
            if (socket == null)
                return;

            try
            {
                var packetData = packet.Data;
                int packetLength = packet.Length;
                lock (sendSyncObj)
                {
                    socket.BeginSend(packetData, 0, packetLength, SocketFlags.None, OnDataSent, socket);
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
                socket.EndSend(ar);
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
        }

        private void SendBitField()
        {
            // Ignore if we have already sent the bit field or if we haven't sent the handshake yet
            if (isBitFieldSent || !isHandshakeSent)
                return;

            var bitField = torrent.BitField;
            var bitFieldBytes = bitField.Buffer;
            int length = bitField.ByteLength + 1;
            var packet = CreatePacket(MessageType.BitField, length);
            packet.Write(bitFieldBytes, 0, bitField.ByteLength);
            SendPacket(packet);

            isBitFieldSent = true;
        }

        private Packet CreatePacket(MessageType messageType, int length)
        {
            if (messageType < 0)
                throw new ArgumentException("The message type is invalid.", "messageType");
            else if (length < 1)
                throw new ArgumentOutOfRangeException("The length has to be at least 1.", "length");

            var packet = new Packet(4 + length);
            packet.WriteInt32(length);
            packet.WriteByte((byte)messageType);
            return packet;
        }
        #endregion

        #region Handle Packets
        private bool HandlePacket(Packet packet)
        {
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
            else if (torrent == null)
            {
                var foundTorrent = TorrentRegistry.FindTorrentByInfoHash(infoHash);
                if (foundTorrent == null)
                {
                    Log.LogWarning("[Peer][{0}] Handshake with unknown info hash: {1}", endPoint, infoHash);
                    return false;
                }

                torrent = foundTorrent;
            }

            this.infoHash = infoHash;
            this.peerID = peerID;
            isHandshakeReceived = true;

            Log.LogDebug("[Peer][{0}] A peer handshaked with us with info hash [{1}] and peer ID [{2}].", endPoint, infoHash, peerID);

            SendHandshake();
            SendBitField();
            return true;
        }

        private bool HandleKeepAlive(Packet packet)
        {
            // TODO: Implement!
            return true;
        }

        private bool HandleChoke(Packet packet)
        {
            if (packet.Length != 5)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'choke' received with {1} bytes (should have been 5).", endPoint, packet.Length);
                return false;
            }
            else if (isChoked)
            {
                Log.LogDebug("[Peer][{0}] A 'choke' was received while already being choked.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer choked us.", endPoint);
            isChoked = true;
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
            else if (!isChoked)
            {
                Log.LogDebug("[Peer][{0}] A 'unchoke' was received while already not being choked.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer unchoked us.", endPoint);
            isChoked = false;
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
            else if (isInterested)
            {
                Log.LogDebug("[Peer][{0}] An 'interested' was received while already being interested.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer is interested.", endPoint);
            isInterested = true;
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
            else if (!isInterested)
            {
                Log.LogDebug("[Peer][{0}] A 'not interested' was received while already not being interested.", endPoint);
                return true;
            }

            Log.LogDebug("[Peer][{0}] Peer is no longer interested.", endPoint);
            isInterested = false;
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
            int expectedLength = (1 + bitFieldByteCount);
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

            int index = packet.ReadInt32();
            int begin = packet.ReadInt32();
            int length = packet.ReadInt32();

            if (index < 0 || begin < 0 || length < 0)
            {
                Log.LogWarning("[Peer][{0}] A peer sent a request with invalid arguments. Index: {1}, Begin: {2}, Length: {3}", endPoint, index, begin, length);
                return false;
            }
            else if (length > MaximumAllowedRequestSize)
            {
                Log.LogWarning("[Peer][{0}] A peer requested more data than allowed. {1} bytes was requested.", endPoint, length);
                return false;
            }

            Log.LogDebug("[Peer][{0}] A peer sent a request to us. Index: {1}, Begin: {2}, Length: {3}", endPoint, index, begin, length);

            // TODO: Implement!
            return true;
        }

        private bool HandlePiece(Packet packet)
        {
            // TODO: Implement!
            return true;
        }

        private bool HandleCancel(Packet packet)
        {
            if (packet.Length != 17)
            {
                Log.LogWarning("[Peer][{0}] Invalid 'cancel' received with {1} bytes (should have been 17).", endPoint, packet.Length);
                return false;
            }

            int index = packet.ReadInt32();
            int begin = packet.ReadInt32();
            int length = packet.ReadInt32();

            // TODO: Implement!
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
