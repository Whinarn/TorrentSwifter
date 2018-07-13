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

        private PeerConnection connection = null;

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
        /// Gets if we are currently connected to this peer.
        /// </summary>
        public bool IsConnected
        {
            get { return (connection != null ? connection.IsConnected : false); }
        }

        /// <summary>
        /// Gets the peer connection.
        /// </summary>
        public PeerConnection Connection
        {
            get { return connection; }
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
            this.connection = null;

            Initialize();
        }

        /// <summary>
        /// Creates a new torrent peer.
        /// </summary>
        /// <param name="torrent">The owner torrent.</param>
        /// <param name="connection">The peer connection.</param>
        internal Peer(Torrent torrent, PeerConnection connection)
        {
            this.torrent = torrent;
            this.endPoint = connection.EndPoint;

            this.connection = connection;
            bitField = this.connection.RemoteBitField;

            Initialize();
            InitializeConnection(connection);
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
            Disconnect();
            Uninitialize();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to connect to this peer synchronously.
        /// </summary>
        /// <returns>If the connection was successfully established.</returns>
        public bool Connect()
        {
            if (connection != null)
                return false;

            try
            {
                connection = new PeerConnectionTCP(torrent, this, endPoint);
                connection.Connect();
                return connection.IsConnected;
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
            if (connection != null)
                return false;

            try
            {
                connection = new PeerConnectionTCP(torrent, this, endPoint);
                await connection.ConnectAsync();
                return connection.IsConnected;
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
            if (connection != null)
            {
                connection.Connected -= OnConnectionConnected;
                connection.ConnectionFailed -= OnConnectionAttemptFailed;
                connection.Disconnected -= OnConnectionDisconnected;
                connection.BitFieldReceived -= OnConnectionBitFieldReceived;
                connection.HavePiece -= OnTCPConnectionHavePiece;

                connection.Disconnect();
                connection.Dispose();
                connection = null;
            }
        }

        /// <summary>
        /// Returns the hash code of this peer.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return endPoint.GetHashCode();
        }

        /// <summary>
        /// Returns the text-representation of this peer.
        /// </summary>
        /// <returns>The peer text-representation.</returns>
        public override string ToString()
        {
            return string.Format("[{0} - {1}]", endPoint, peerID);
        }
        #endregion

        #region Internal Methods
        internal void ReplaceConnection(PeerConnection connection)
        {
            if (this.connection == connection) // We already have this connection
                return;

            Disconnect();
            this.connection = connection;
            InitializeConnection(connection);
        }

        internal void Update()
        {
            if (connection != null)
            {
                connection.Update();
            }
        }

        internal async Task SendPieceData(int pieceIndex, int begin, byte[] data)
        {
            if (connection == null)
                return;

            await connection.SendPieceData(pieceIndex, begin, data);
        }
        #endregion

        #region Private Methods
        #region Initialize & Uninitialize
        private void Initialize()
        {
            torrent.PieceVerified += OnTorrentPieceVerified;
        }

        private void InitializeConnection(PeerConnection connection)
        {
            connection.Connected += OnConnectionConnected;
            connection.ConnectionFailed += OnConnectionAttemptFailed;
            connection.Disconnected += OnConnectionDisconnected;
            connection.BitFieldReceived += OnConnectionBitFieldReceived;
            connection.HavePiece += OnTCPConnectionHavePiece;
        }

        private void Uninitialize()
        {
            torrent.PieceVerified -= OnTorrentPieceVerified;
        }
        #endregion

        #region Torrent Events
        private void OnTorrentPieceVerified(object sender, PieceEventArgs e)
        {
            if (connection != null && connection.IsConnected && connection.IsHandshaked)
            {
                connection.ReportHavePiece(e.PieceIndex);
            }
        }
        #endregion

        #region Connection Events
        private void OnConnectionConnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Connected to {0}", endPoint);
        }

        private void OnConnectionAttemptFailed(object sender, ConnectionFailedEventArgs e)
        {
            Log.LogInfo("[Peer] Connection attempt failed to {0} with reason: {1}", endPoint, e.FailedReason);
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Disconnected from {0}", endPoint);
        }

        private void OnConnectionBitFieldReceived(object sender, BitFieldEventArgs e)
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
        #endregion
    }
}
