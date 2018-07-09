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
            get;
        }

        /// <summary>
        /// Gets if this connection has been established.
        /// </summary>
        public override bool IsConnected
        {
            get;
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
        #endregion

        #region Private Methods
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
                        Logger.LogException(ex, false);
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
                default:
                    Logger.LogError("[Peer] Received unhandled message type: {0}", messageType);
                    Disconnect();
                    break;
            }
        }

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
    }
}
