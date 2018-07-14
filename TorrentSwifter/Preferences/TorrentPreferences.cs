using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// Torrent preferences.
    /// </summary>
    [Serializable]
    public sealed class TorrentPreferences
    {
        #region Fields
        private bool allocateFullFileSizes = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if the full file sizes are allocated for new torrent downloads.
        /// </summary>
        public bool AllocateFullFileSizes
        {
            get { return allocateFullFileSizes; }
            set { allocateFullFileSizes = value; }
        }
        #endregion
    }
}
