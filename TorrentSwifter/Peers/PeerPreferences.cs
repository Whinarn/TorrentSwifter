using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Peer preferences.
    /// </summary>
    public class PeerPreferences
    {
        #region Fields
        private int listenPort = 0;
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
        #endregion
    }
}
