using System;
using TorrentSwifter.Preferences;

namespace TorrentSwifter.Torrents.RateLimiter
{
    /// <summary>
    /// A rate limiter that attempts to keep the bandwidth under a certain limit.
    /// </summary>
    public sealed class BandwidthLimiter : IRateLimiter
    {
        private readonly RateMeasurer rateMeasurer;
        private long rateLimit;

        /// <summary>
        /// Gets or sets the rate limit for this limiter.
        /// </summary>
        public long RateLimit
        {
            get { return rateLimit; }
            set { rateLimit = value; }
        }

        /// <summary>
        /// Creates a bandwidth limiter.
        /// </summary>
        /// <param name="rateMeasurer">The rate measurer to use.</param>
        /// <param name="rateLimit">The maximum rate before limiting.</param>
        public BandwidthLimiter(RateMeasurer rateMeasurer, long rateLimit)
        {
            if (rateMeasurer == null)
                throw new ArgumentNullException("rateMeasurer");

            this.rateMeasurer = rateMeasurer;
            this.rateLimit = rateLimit;
        }

        /// <summary>
        /// Attempts to process a certain amount through this rate limiter.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>If the rate limiter allows us to process.</returns>
        public bool TryProcess(long amount)
        {
            long newRate = (rateMeasurer.AverageRate + amount);
            return (newRate <= rateLimit);
        }
    }
}
