using System;

namespace TorrentSwifter
{
    /// <summary>
    /// Disk preferences.
    /// </summary>
    [Serializable]
    public sealed class DiskPreferences
    {
        #region Fields
        private int maxQueuedWrites = 20;
        private int maxConcurrentWrites = 1;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the maximum count of queued writes to disk.
        /// </summary>
        public int MaxQueuedWrites
        {
            get { return maxQueuedWrites; }
            set { maxQueuedWrites = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of concurrent writes to disk.
        /// </summary>
        public int MaxConcurrentWrites
        {
            get { return maxConcurrentWrites; }
            set { maxConcurrentWrites = Math.Max(value, 1); }
        }
        #endregion
    }
}
