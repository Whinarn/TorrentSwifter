using System;
using System.Threading;
using TorrentSwifter.Preferences;
using TorrentSwifter.Torrents;
using TorrentSwifter.Torrents.RateLimiter;

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

        private static RateMeasurer downloadRate = new RateMeasurer();
        private static RateMeasurer uploadRate = new RateMeasurer();

        internal static readonly BandwidthLimiter downloadRateLimiter;
        internal static readonly BandwidthLimiter uploadRateLimiter;
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

        /// <summary>
        /// Gets the average download rate in bytes per second for all combined downloading of all active torrents.
        /// </summary>
        public static long DownloadRate
        {
            get { return downloadRate.AverageRate; }
        }

        /// <summary>
        /// Gets the average upload rate in bytes per second for all combined uploading of all active torrents.
        /// </summary>
        public static long UploadRate
        {
            get { return uploadRate.AverageRate; }
        }
        #endregion

        #region Static Initializer
        static Stats()
        {
            long downloadBandwidthLimit = Prefs.Torrent.DownloadBandwidthLimit;
            long uploadBandwidthLimit = Prefs.Torrent.UploadBandwidthLimit;
            downloadRateLimiter = new BandwidthLimiter(downloadRate, downloadBandwidthLimit);
            uploadRateLimiter = new BandwidthLimiter(uploadRate, uploadBandwidthLimit);
        }
        #endregion

        #region Internal Methods
        internal static void IncreaseDownloadedBytes(long amount)
        {
            Interlocked.Add(ref totalDownloadedBytes, amount);
            downloadRate.Add(amount);
        }

        internal static void IncreaseUploadedBytes(long amount)
        {
            Interlocked.Add(ref totalUploadedBytes, amount);
            uploadRate.Add(amount);
        }

        internal static void Update()
        {
            downloadRate.Update();
            uploadRate.Update();

            // Update the global rate limiters in case the limits have changed
            // TODO: Can we do this bit smarter?
            long downloadBandwidthLimit = Prefs.Torrent.DownloadBandwidthLimit;
            long uploadBandwidthLimit = Prefs.Torrent.UploadBandwidthLimit;
            downloadRateLimiter.RateLimit = downloadBandwidthLimit;
            uploadRateLimiter.RateLimit = uploadBandwidthLimit;
        }
        #endregion
    }
}
