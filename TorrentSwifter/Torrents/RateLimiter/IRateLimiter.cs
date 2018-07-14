using System;

namespace TorrentSwifter.Torrents.RateLimiter
{
    /// <summary>
    /// A limiter that tries to maintain a specific rate used for example when downloading or downloading.
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Attempts to process a certain amount through this rate limiter.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>If the rate limiter allows us to process.</returns>
        bool TryProcess(long amount);
    }
}
