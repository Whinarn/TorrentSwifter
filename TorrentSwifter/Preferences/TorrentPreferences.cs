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

        private long downloadBandwidthLimit = 0L;
        private long uploadBandwidthLimit = 0L;
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

        /// <summary>
        /// Gets or sets the download bandwidth limit in bytes per second for the combined downloading from all active torrents.
        /// Zero means that no limit is imposed.
        /// </summary>
        public long DownloadBandwidthLimit
        {
            get { return downloadBandwidthLimit; }
            set { downloadBandwidthLimit = Math.Max(value, 0L); }
        }

        /// <summary>
        /// Gets or sets the upload bandwidth limit in bytes per second for the combined uploading from all active torrents.
        /// Zero means that no limit is imposed.
        /// </summary>
        public long UploadBandwidthLimit
        {
            get { return uploadBandwidthLimit; }
            set { uploadBandwidthLimit = Math.Max(value, 0L); }
        }
        #endregion
    }
}
