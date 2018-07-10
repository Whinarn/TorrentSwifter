using System;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter
{
    /// <summary>
    /// A collection of all preferences.
    /// </summary>
    public static class Preferences
    {
        #region Fields
        private static PeerPreferences peer = new PeerPreferences();
        private static TorrentPreferences torrent = new TorrentPreferences();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the peer preferences.
        /// </summary>
        public static PeerPreferences Peer
        {
            get { return peer; }
        }

        /// <summary>
        /// Gets the torrent preferences.
        /// </summary>
        public static TorrentPreferences Torrent
        {
            get { return torrent; }
        }
        #endregion
    }
}
