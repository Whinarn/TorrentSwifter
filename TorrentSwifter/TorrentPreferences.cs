using System;

namespace TorrentSwifter
{
    /// <summary>
    /// Torrent preferences.
    /// </summary>
    public static class TorrentPreferences
    {
        #region Fields
        private static bool allocateFullFileSizes = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if the full file sizes are allocated for new torrent downloads.
        /// </summary>
        public static bool AllocateFullFileSizes
        {
            get { return allocateFullFileSizes; }
            set { allocateFullFileSizes = value; }
        }
        #endregion
    }
}
