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
        /// Zero means unlimited.
        /// </summary>
        public long RateLimit
        {
            get { return rateLimit; }
            set { rateLimit = Math.Max(value, 0L); }
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
            // Check if a limitation is not imposed
            if (rateLimit <= 0L)
                return true;

            long newRate = (rateMeasurer.AverageRate + amount);
            return (newRate <= rateLimit);
        }
    }
}
