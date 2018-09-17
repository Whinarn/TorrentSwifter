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
using System.Collections;
using System.Collections.Generic;

namespace TorrentSwifter.Torrents.RateLimiter
{
    /// <summary>
    /// A group of rate limiters.
    /// </summary>
    public sealed class RateLimiterGroup : IRateLimiter, IEnumerable<IRateLimiter>, IEnumerable
    {
        #region Fields
        private List<IRateLimiter> limiters;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the count of rate limiters in this group.
        /// </summary>
        public int Count
        {
            get { return limiters.Count; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a group of rate limiters.
        /// </summary>
        public RateLimiterGroup()
        {
            limiters = new List<IRateLimiter>();
        }

        /// <summary>
        /// Creates a group of rate limiters.
        /// </summary>
        /// <param name="limiters">The initial group of limiters.</param>
        public RateLimiterGroup(IEnumerable<IRateLimiter> limiters)
        {
            if (limiters == null)
                throw new ArgumentNullException("limiters");

            limiters = new List<IRateLimiter>(limiters);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new rate limiter to this group.
        /// </summary>
        /// <param name="limiter">The rate limiter.</param>
        public void Add(IRateLimiter limiter)
        {
            if (limiter == null)
                throw new ArgumentNullException("limiter");

            if (!limiters.Contains(limiter))
            {
                limiters.Add(limiter);
            }
        }

        /// <summary>
        /// Returns if this group contains a specific rate limiter.
        /// </summary>
        /// <param name="limiter">The rate limiter.</param>
        /// <returns>If the rate limiter exists in the group.</returns>
        public bool Contains(IRateLimiter limiter)
        {
            if (limiter == null)
                throw new ArgumentNullException("limiter");

            return limiters.Contains(limiter);
        }

        /// <summary>
        /// Removes a rate limiter to this group.
        /// </summary>
        /// <param name="limiter">The rate limiter.</param>
        /// <returns>If successfully removed.</returns>
        public bool Remove(IRateLimiter limiter)
        {
            if (limiter == null)
                throw new ArgumentNullException("limiter");

            return limiters.Remove(limiter);
        }

        /// <summary>
        /// Clears this group of rate limiters.
        /// </summary>
        public void Clear()
        {
            limiters.Clear();
        }

        /// <summary>
        /// Attempts to process a certain amount through this rate limiter.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>If the rate limiter allows us to process.</returns>
        public bool TryProcess(long amount)
        {
            bool result = true;

            for (int i = 0, limiterCount = limiters.Count; i < limiterCount; i++)
            {
                if (!limiters[i].TryProcess(amount))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the enumerator of rate limiters in this group.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<IRateLimiter> GetEnumerator()
        {
            return limiters.GetEnumerator();
        }
        #endregion

        #region Private Methods
        IEnumerator IEnumerable.GetEnumerator()
        {
            return limiters.GetEnumerator();
        }
        #endregion
    }
}
