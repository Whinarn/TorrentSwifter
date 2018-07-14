using System;
using TorrentSwifter.Managers;
using TorrentSwifter.Preferences;

namespace TorrentSwifter.Torrents.RateLimiter
{
    /// <summary>
    /// A rate limiter that prevents making too many disk reads.
    /// </summary>
    public sealed class DiskReadLimiter : IRateLimiter
    {
        /// <summary>
        /// Attempts to process a certain amount through this rate limiter.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>If the rate limiter allows us to process.</returns>
        public bool TryProcess(long amount)
        {
            int queuedReads = DiskManager.QueuedReads;
            return (queuedReads < Prefs.Disk.MaxQueuedReads);
        }
    }
}
