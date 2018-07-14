using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// Peer preferences.
    /// </summary>
    [Serializable]
    public sealed class PeerPreferences
    {
        #region Fields
        private int listenPort = 0;

        private int handshakeTimeout = 15 * 1000; // 15 seconds
        private int inactiveTimeout = 4 * 60 * 1000; // 4 minutes
        private int pieceRequestTimeout = 60 * 1000; // 1 minute

        private int maxDownloadConnections = 15;
        private int maxUploadConnections = 5;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the port to listen for new connections on.
        /// Zero means that any available port selected by the OS will be used.
        /// </summary>
        public int ListenPort
        {
            get { return listenPort; }
            set
            {
                if (value < 1 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException("value");

                listenPort = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout (in milliseconds) before disconnecting peers that haven't yet sent an handshake.
        /// Zero means that there is no timeout.
        /// </summary>
        public int HandshakeTimeout
        {
            get { return handshakeTimeout; }
            set { handshakeTimeout = Math.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the timeout (in milliseconds) before disconnecting peers that haven't been active.
        /// Zero means that there is no timeout.
        /// </summary>
        public int InactiveTimeout
        {
            get { return inactiveTimeout; }
            set { inactiveTimeout = Math.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the timeout (in milliseconds) before we cancel a piece request that hasn't yet been fulfilled.
        /// Zero means that there is no timeout.
        /// </summary>
        public int PieceRequestTimeout
        {
            get { return pieceRequestTimeout; }
            set { pieceRequestTimeout = Math.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the maximum count of connections that we allow to download from at the same time.
        /// </summary>
        public int MaxDownloadConnections
        {
            get { return maxDownloadConnections; }
            set { maxDownloadConnections = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of connections that we allow to upload to at the same time.
        /// </summary>
        public int MaxUploadConnections
        {
            get { return maxUploadConnections; }
            set { maxUploadConnections = Math.Max(value, 1); }
        }
        #endregion
    }
}
