﻿using System;
using System.Net;
using System.Threading.Tasks;
using TorrentSwifter.Collections;
using TorrentSwifter.Logging;
using TorrentSwifter.Preferences;
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
        private bool haveTriedConnectTo = false;

        private bool isCompleted = false;
        private BitField bitField = null;

        private ConcurrentList<OutgoingPieceRequest> pieceRequests = new ConcurrentList<OutgoingPieceRequest>();
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
        /// Gets if this peer has completed downloading.
        /// </summary>
        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        /// <summary>
        /// Gets if we can request pieces from this peer.
        /// </summary>
        public bool CanRequestPiecesFrom
        {
            get
            {
                return (connection != null && connection.IsInterestedByUs && !connection.IsChokedByRemote &&
                    pieceRequests.Count <= Prefs.Peer.MaxConcurrentPieceRequests);
            }
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
            haveTriedConnectTo = true;
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
                connection.HavePiece -= OnConnectionHavePiece;
                connection.StateChanged -= OnConnectionStateChanged;

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
            if (connection != null && connection.IsConnected)
            {
                connection.Update();
                if (!connection.IsConnected) // If we just disconnected
                    return;

                // If both we and the peer has completed, we simply disconnect from the peer
                // because there is nothing left to do.
                if (torrent.IsCompleted && isCompleted)
                {
                    connection.Disconnect();
                    return;
                }

                // TODO: Add a more clever choke/unchoke algorithm
                if (connection.IsInterestedByRemote && connection.IsChokedByUs)
                {
                    connection.SendChoked(false);
                }
                else if (!connection.IsInterestedByRemote && !connection.IsChokedByUs)
                {
                    connection.SendChoked(true);
                }

                if (torrent.IsCompleted)
                {
                    if (connection.IsInterestedByUs)
                    {
                        connection.SendInterested(false);
                    }
                }
                else
                {
                    // TODO: Add a more clever interest algorithm
                    if (!connection.IsInterestedByUs)
                    {
                        int neededPieceCount = GetCountOfNeededPieces();
                        if (neededPieceCount > 0)
                        {
                            connection.SendInterested(true);
                        }
                    }
                }
            }
            else if (connection == null || !connection.IsConnecting)
            {
                if (!haveTriedConnectTo)
                {
                    haveTriedConnectTo = true;
                    Connect();
                }
            }
        }

        internal async Task<bool> RequestPieceData(int pieceIndex, int blockIndex)
        {
            if (connection == null)
                return false;

            return await connection.RequestPieceData(pieceIndex, blockIndex);
        }

        internal void CancelPieceDataRequest(int pieceIndex, int blockIndex)
        {
            if (connection == null)
                return;

            connection.CancelPieceDataRequest(pieceIndex, blockIndex);
        }

        internal async Task SendPieceData(int pieceIndex, int begin, byte[] data)
        {
            if (connection == null)
                return;

            await connection.SendPieceData(pieceIndex, begin, data);
        }

        internal void RegisterPieceRequest(OutgoingPieceRequest request)
        {
            pieceRequests.TryAdd(request);
        }

        internal void UnregisterPieceRequest(OutgoingPieceRequest request)
        {
            pieceRequests.Remove(request);
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
            connection.HavePiece += OnConnectionHavePiece;
            connection.StateChanged += OnConnectionStateChanged;
        }

        private void Uninitialize()
        {
            torrent.PieceVerified -= OnTorrentPieceVerified;
        }
        #endregion

        #region Pieces
        private int GetCountOfNeededPieces()
        {
            if (bitField == null)
                return 0;

            var ourBitField = torrent.BitField;
            return ourBitField.CountNeeded(bitField);
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

        #region Peer Events
        private void OnPeerCompleted()
        {
            
        }
        #endregion

        #region Connection Events
        private void OnConnectionConnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Connected to {0}", endPoint);
        }

        private void OnConnectionAttemptFailed(object sender, PeerConnectionFailedEventArgs e)
        {
            Log.LogInfo("[Peer] Connection attempt failed to {0} with reason: {1}", endPoint, e.FailedReason);
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            Log.LogInfo("[Peer] Disconnected from {0}", endPoint);

            if (torrent != null)
            {
                torrent.OnPeerDisconnected(this);
            }
        }

        private void OnConnectionBitFieldReceived(object sender, BitFieldEventArgs e)
        {
            bitField = e.BitField;

            bool isCompleted = bitField.HasAllSet();
            if (isCompleted != this.isCompleted)
            {
                this.isCompleted = true;
                OnPeerCompleted();
            }
        }

        private void OnConnectionHavePiece(object sender, PieceEventArgs e)
        {
            if (bitField == null)
            {
                bitField = new BitField(torrent.PieceCount);
            }

            bitField.Set(e.PieceIndex, true);

            bool isCompleted = bitField.HasAllSet();
            if (isCompleted != this.isCompleted)
            {
                this.isCompleted = true;
                OnPeerCompleted();
            }
        }

        private void OnConnectionStateChanged(object sender, PeerConnectionStateEventArgs e)
        {
            if (torrent != null && e.IsChoked)
            {
                torrent.OnPeerChokingUs(this);
            }
        }
        #endregion
        #endregion
    }
}
