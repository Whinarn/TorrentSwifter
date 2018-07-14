using System;
using TorrentSwifter.Managers;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter
{
    /// <summary>
    /// The engine for the entire TorrentSwifter library.
    /// </summary>
    public static class TorrentEngine
    {
        #region Fields
        private static bool isInitialized = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets if the torrent engine has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get { return isInitialized; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the torrent engine.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            PeerListener.StartListening();
        }

        /// <summary>
        /// Uninitializes the torrent engine.
        /// </summary>
        public static void Uninitialize()
        {
            if (!isInitialized)
                return;

            isInitialized = false;

            TorrentRegistry.StopAllActiveTorrents();
            PeerListener.StopListening();
        }
        #endregion
    }
}
