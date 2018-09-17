#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

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
