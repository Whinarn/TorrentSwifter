﻿using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// A collection of all preferences.
    /// </summary>
    public static class Prefs
    {
        #region Fields
        [PreferenceSection("Disk")]
        private static DiskPreferences disk = new DiskPreferences();
        [PreferenceSection("Peer")]
        private static PeerPreferences peer = new PeerPreferences();
        [PreferenceSection("Torrent")]
        private static TorrentPreferences torrent = new TorrentPreferences();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the disk preferences.
        /// </summary>
        public static DiskPreferences Disk
        {
            get { return disk; }
        }

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
