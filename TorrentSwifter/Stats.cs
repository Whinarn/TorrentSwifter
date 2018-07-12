using System;
using System.Threading;

namespace TorrentSwifter
{
    /// <summary>
    /// BitTorrent statistics.
    /// </summary>
    public static class Stats
    {
        #region Fields
        private static long totalDownloadedBytes = 0L;
        private static long totalUploadedBytes = 0L;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the total amount of bytes downloaded.
        /// </summary>
        public static long TotalDownloadedBytes
        {
            get { return totalDownloadedBytes; }
        }

        /// <summary>
        /// Gets the total amount of bytes uploaded.
        /// </summary>
        public static long TotalUploadedBytes
        {
            get { return totalUploadedBytes; }
        }
        #endregion

        #region Internal Methods
        internal static void IncreaseDownloadedBytes(long amount)
        {
            Interlocked.Add(ref totalDownloadedBytes, amount);
        }

        internal static void IncreaseUploadedBytes(long amount)
        {
            Interlocked.Add(ref totalUploadedBytes, amount);
        }
        #endregion
    }
}
