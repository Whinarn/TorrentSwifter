using System;
using TorrentSwifter.Managers;
using TorrentSwifter.Preferences;

namespace TorrentSwifter.Torrents.RateLimiter
{
    /// <summary>
    /// A rate limiter that prevents making too many disk writes.
    /// </summary>
    public sealed class DiskWriteLimiter : IRateLimiter
    {
        /// <summary>
        /// Attempts to process a certain amount through this rate limiter.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>If the rate limiter allows us to process.</returns>
        public bool TryProcess(long amount)
        {
            int queuedWrites = DiskManager.QueuedWrites;
            return (queuedWrites < Prefs.Disk.MaxQueuedWrites);
        }
    }
}
