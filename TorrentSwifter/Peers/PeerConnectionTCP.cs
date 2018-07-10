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
        private bool isHandshakeReceived = false;
        private bool isChoked = true;
        private bool isInterested = false;

        private InfoHash infoHash = default(InfoHash);
        private PeerID peerID = default(PeerID);

        private int receiveOffset = 0;
        private byte[] receiveBuffer = new byte[ReceiveBufferMaxSize];
        private Packet receivedPacket = null;
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
        /// <param name="endPoint">The peer IP end-point.</param>
        public PeerConnectionTCP(IPEndPoint endPoint)
            : base(endPoint)
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            receivedPacket = new Packet(receiveBuffer, 0);
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
                StartDataReceive();
                OnConnected();
            }
            catch (SocketException ex)
            {
                var failedReason = GetConnectionFailedReason(ex.SocketErrorCode);
                OnConnectionFailed(failedReason);
            }
            catch (Exception ex)
            {
                Logger.LogErrorException(ex);
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
                StartDataReceive();
                OnConnected();
            }
            catch (SocketException ex)
            {
                var failedReason = GetConnectionFailedReason(ex.SocketErrorCode);
                OnConnectionFailed(failedReason);
            }
            catch (Exception ex)
            {
                Logger.LogErrorException(ex);
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

            base.OnConnected();
        }

        /// <summary>
        /// The peer connection has been disconnected.
        /// </summary>
        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            isHandshakeReceived = false;
        }
        #endregion

        #region Private Methods
        #region Data Receiving
        private void StartDataReceive()
        {
            if (socket == null)
                return;

            if (receiveOffset >= receiveBuffer.Length)
            {
                // There is nothing more to receive in the buffer, and to prevent getting stuck, we simply reset the receive offset
                // This should never happen though...
                receiveOffset = 0;
            }

            try
            {
                socket.BeginReceive(receiveBuffer, receiveOffset, receiveBuffer.Length - receiveOffset, SocketFlags.None, OnDataReceived, socket);
            }
            catch (ObjectDisposedException)
            {
                // NOTE: If the socket as been disposed of, then we can safely discard this exception and just stop listen
            }
            catch
            {
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
                        HandlePacket(receivedPacket);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorException(ex);
                        Disconnect();
                    }

                    // Copy remaining bytes in the buffer to the start of the buffer
                    Buffer.BlockCopy(receiveBuffer, packetLength, receiveBuffer, 0, receiveOffset - packetLength);
                    receiveOffset = 0;
                    packetLength = GetPacketLength();
                }

                if (packetLength > ReceiveBufferMaxSize)
                {
                    Logger.LogError("[Peer] A peer [{0}] sent a packet that is too large to handle ({1} > {2}). Exploiter?", endPoint, packetLength, ReceiveBufferMaxSize);
                    Disconnect();
                }
            }
            finally
            {
                StartDataReceive();
            }
        }
        #endregion

        #region Handle Packets
        private void HandlePacket(Packet packet)
        {
            var messageType = GetMessageType(packet);
            if (messageType == MessageType.Unknown)
            {
                Logger.LogWarning("[Peer] Received unhandled message of {0} bytes.", packet.Length);
                return;
            }

            switch (messageType)
            {
                case MessageType.Handshake:
                    HandleHandshake(packet);
                    break;
                case MessageType.KeepAlive:
                    HandleKeepAlive(packet);
                    break;
                case MessageType.Choke:
                    HandleChoke(packet);
                    break;
                case MessageType.Unchoke:
                    HandleUnchoke(packet);
                    break;
                case MessageType.Interested:
                    HandleInterested(packet);
                    break;
                case MessageType.NotInterested:
                    HandleNotInterested(packet);
                    break;
                case MessageType.Have:
                    HandleHave(packet);
                    break;
                case MessageType.BitField:
                    HandleBitField(packet);
                    break;
                case MessageType.Request:
                    HandleRequest(packet);
                    break;
                case MessageType.Piece:
                    HandlePiece(packet);
                    break;
                case MessageType.Cancel:
                    HandleCancel(packet);
                    break;
                default:
                    Logger.LogError("[Peer] Received unhandled message type: {0}", messageType);
                    Disconnect();
                    break;
            }
        }

        private void HandleHandshake(Packet packet)
        {
            if (packet.Length != 68)
            {
                Logger.LogWarning("[Peer] Invalid handshake received with {0} bytes.", packet.Length);
                Disconnect();
                return;
            }

            int protocolNameLength = packet.ReadByte();
            if (packet.RemainingBytes < (protocolNameLength + 40))
            {
                Logger.LogWarning("[Peer] Invalid handshake received with {0} bytes (protocol name length: {1}).", packet.Length, protocolNameLength);
                Disconnect();
                return;
            }

            string protocolName = packet.ReadString(protocolNameLength);
            if (!string.Equals(protocolName, "BitTorrent protocol"))
            {
                Logger.LogWarning("[Peer] Handshake protocol is not supported: {0}", protocolName);
                Disconnect();
                return;
            }

            // Skip 8 bytes of flags
            packet.Skip(8);

            byte[] hashBytes = packet.ReadBytes(20);
            byte[] idBytes = packet.ReadBytes(20);

            infoHash = new InfoHash(hashBytes);
            peerID = new PeerID(idBytes);
            isHandshakeReceived = true;

            // TODO: Send bit field!
        }

        private void HandleKeepAlive(Packet packet)
        {
            // TODO: Implement!
        }

        private void HandleChoke(Packet packet)
        {
            if (isChoked)
                return;

            Logger.LogInfo("[Peer] Choked by {0}", endPoint);
            isChoked = true;
            OnStateChanged();
        }

        private void HandleUnchoke(Packet packet)
        {
            if (!isChoked)
                return;

            Logger.LogInfo("[Peer] Unchoked by {0}", endPoint);
            isChoked = false;
            OnStateChanged();
        }

        private void HandleInterested(Packet packet)
        {
            if (isInterested)
                return;

            Logger.LogInfo("[Peer] {0} is interested.", endPoint);
            isInterested = true;
            OnStateChanged();
        }

        private void HandleNotInterested(Packet packet)
        {
            if (!isInterested)
                return;

            Logger.LogInfo("[Peer] {0} is no longer interested.", endPoint);
            isInterested = false;
            OnStateChanged();
        }

        private void HandleHave(Packet packet)
        {
            // TODO: Implement!
        }

        private void HandleBitField(Packet packet)
        {
            // TODO: Implement!
        }

        private void HandleRequest(Packet packet)
        {
            // TODO: Implement!
        }

        private void HandlePiece(Packet packet)
        {
            // TODO: Implement!
        }

        private void HandleCancel(Packet packet)
        {
            // TODO: Implement!
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
