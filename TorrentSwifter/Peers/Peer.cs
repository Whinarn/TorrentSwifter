using System;
using System.Net;
using System.Threading.Tasks;
using TorrentSwifter.Logging;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// A torrent peer.
    /// </summary>
    public sealed class Peer : IDisposable
    {
        #region Fields
        private readonly Torrent torrent;
        private readonly EndPoint endPoint;
        private PeerID peerID = PeerID.None;

        private PeerConnectionTCP tcpConnection = null;

        private BitField bitField = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the owner torrent for this peer.
        /// </summary>
        public Torrent Torrent
        {
            get { return torrent; }
        }

        /// <summary>
        /// Gets the end-point of the peer.
        /// </summary>
        public EndPoint EndPoint
        {
            get { return endPoint; }
        }

        /// <summary>
        /// Gets the ID of this peer.
        /// </summary>
        public PeerID ID
        {
            get { return peerID; }
        }

        /// <summary>
        /// Gets the peer TCP connection.
        /// </summary>
        public PeerConnectionTCP TCPConnection
        {
            get { return tcpConnection; }
        }

        /// <summary>
        /// Gets the bit field for this peer.
        /// Note that this can be null before it has been received.
        /// </summary>
        public BitField BitField
        {
            get { return bitField; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new torrent peer.
        /// </summary>
        /// <param name="torrent">The owner torrent.</param>
        /// <param name="endPoint">The peer end-point.</param>
        internal Peer(Torrent torrent, EndPoint endPoint)
        {
            this.torrent = torrent;
            this.endPoint = endPoint;

            tcpConnection = new PeerConnectionTCP(torrent, endPoint);
            tcpConnection.Connected += OnTCPConnectionConnected;
            tcpConnection.ConnectionFailed += OnTCPConnectionAttemptFailed;
            tcpConnection.Disconnected += OnTCPConnectionDisconnected;
            tcpConnection.BitFieldReceived += OnTCPConnectionBitFieldReceived;
            tcpConnection.HavePiece += OnTCPConnectionHavePiece;
        }
        #endregion

        #region Finalizer
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Peer()
        {
            Dispose(false);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Disposes of this peer.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Disposes of this peer connection.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        private void Dispose(bool disposing)
        {
            if (tcpConnection != null)
            {
                tcpConnection.Connected -= OnTCPConnectionConnected;
                tcpConnection.ConnectionFailed -= OnTCPConnectionAttemptFailed;
                tcpConnection.Disconnected -= OnTCPConnectionDisconnected;
                tcpConnection.BitFieldReceived -= OnTCPConnectionBitFieldReceived;
                tcpConnection.HavePiece -= OnTCPConnectionHavePiece;
                tcpConnection.Dispose();
                tcpConnection = null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to connect to this peer synchronously.
        /// </summary>
        /// <returns>If the connection was successfully established.</returns>
        public bool Connect()
        {
            if (tcpConnection == null)
                return false;
            else if (tcpConnection.IsConnecting || tcpConnection.IsConnected)
                return false;

            try
            {
                tcpConnection.Connect();
                return tcpConnection.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to connect to this peer asynchronously.
        /// </summary>
        /// <returns>If the connection was successfully established.</returns>
        public async Task<bool> ConnectAsync()
        {
            if (tcpConnection == null)
                return false;
            else if (tcpConnection.IsConnecting || tcpConnection.IsConnected)
                return false;

            try
            {
                await tcpConnection.ConnectAsync();
                return tcpConnection.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects from this peer.
        /// </summary>
        public void Disconnect()
        {
            if (tcpConnection.IsConnected || tcpConnection.IsConnecting)
            {
                tcpConnection.Disconnect();
            }
        }

        /// <summary>
        /// Returns the text-representation of this peer.
        /// </summary>
        /// <returns>The peer text-representation.</returns>
        public override string ToString()
        {
            // TODO: Expand this
            return endPoint.ToString();
        }
        #endregion

        #region Connection Events
        private void OnTCPConnectionConnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Connected to {0}", endPoint);
        }

        private void OnTCPConnectionAttemptFailed(object sender, ConnectionFailedEventArgs e)
        {
            Log.LogInfo("[Peer] Connection attempt failed to {0} with reason: {1}", endPoint, e.FailedReason);
        }

        private void OnTCPConnectionDisconnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Disconnected from {0}", endPoint);
        }

        private void OnTCPConnectionBitFieldReceived(object sender, BitFieldEventArgs e)
        {
            bitField = e.BitField;
        }

        private void OnTCPConnectionHavePiece(object sender, PieceEventArgs e)
        {
            if (bitField == null)
            {
                bitField = new BitField(torrent.PieceCount);
            }

            bitField.Set(e.PieceIndex, true);
        }
        #endregion
    }
}
