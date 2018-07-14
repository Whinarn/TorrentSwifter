using System;

namespace TorrentSwifter.Preferences
{
    /// <summary>
    /// Disk preferences.
    /// </summary>
    [Serializable]
    public sealed class DiskPreferences
    {
        #region Fields
        private int maxQueuedReads = 30;
        private int maxQueuedWrites = 20;

        private int maxConcurrentReads = 1;
        private int maxConcurrentWrites = 1;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the maximum count of queued reads from the disk.
        /// </summary>
        public int MaxQueuedReads
        {
            get { return maxQueuedReads; }
            set { maxQueuedReads = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of queued writes to the disk.
        /// </summary>
        public int MaxQueuedWrites
        {
            get { return maxQueuedWrites; }
            set { maxQueuedWrites = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of concurrent reads from the disk.
        /// </summary>
        public int MaxConcurrentReads
        {
            get { return maxConcurrentReads; }
            set { maxConcurrentReads = Math.Max(value, 1); }
        }

        /// <summary>
        /// Gets or sets the maximum count of concurrent writes to the disk.
        /// </summary>
        public int MaxConcurrentWrites
        {
            get { return maxConcurrentWrites; }
            set { maxConcurrentWrites = Math.Max(value, 1); }
        }
        #endregion
    }
}
