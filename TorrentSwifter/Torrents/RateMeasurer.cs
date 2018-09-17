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

namespace TorrentSwifter.Torrents
{
    /// <summary>
    /// A rate measurer.
    /// </summary>
    public sealed class RateMeasurer
    {
        #region Consts
        private const int DefaultAverageRateCount = 10;
        #endregion

        #region Fields
        private long total = 0L;
        private long[] rates = null;
        private int nextRateIndex = 0;
        private int ratesUsed = 0;
        private long averageRate = 0;
        private long lastRate = 0;

        private long current = 0L;
        private DateTime lastUpdateTime = DateTime.UtcNow;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the total amount that has passed through.
        /// </summary>
        public long Total
        {
            get { return Interlocked.Read(ref total); }
        }

        /// <summary>
        /// Gets the average rate.
        /// </summary>
        public long AverageRate
        {
            get { return averageRate; }
        }

        /// <summary>
        /// Gets the last rate.
        /// </summary>
        public long LastRate
        {
            get { return lastRate; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new rate measurer.
        /// </summary>
        public RateMeasurer()
            : this(DefaultAverageRateCount)
        {

        }

        /// <summary>
        /// Creates a new rate measurer.
        /// </summary>
        /// <param name="averageRateCount">The count of rates to use for the average mesaurement.</param>
        public RateMeasurer(int averageRateCount)
        {
            rates = new long[averageRateCount];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds an amount to this measurer.
        /// Make sure that all amounts added is the same unit.
        /// </summary>
        /// <param name="amount">The amount to add.</param>
        public void Add(long amount)
        {
            Interlocked.Add(ref total, amount);
            Interlocked.Add(ref current, amount);
        }

        /// <summary>
        /// Resets the current measurement values.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref total, 0L);
            nextRateIndex = 0;
            ratesUsed = 0;
            averageRate = 0;
            lastRate = 0;

            for (int i = 0; i < rates.Length; i++)
            {
                rates[i] = 0;
            }

            Interlocked.Exchange(ref current, 0L);
            lastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates this measurer.
        /// </summary>
        public void Update()
        {
            var timeNow = DateTime.UtcNow;
            double timeSinceLastUpdate = timeNow.Subtract(lastUpdateTime).TotalSeconds;
            if (timeSinceLastUpdate >= 1.0)
            {
                long current = Interlocked.Exchange(ref this.current, 0L);
                int rate = (int)(current / timeSinceLastUpdate);
                rates[nextRateIndex] = rate;
                lastRate = rate;
                lastUpdateTime = timeNow;

                ++nextRateIndex;
                if (nextRateIndex > ratesUsed)
                {
                    ratesUsed = nextRateIndex;
                }

                if (nextRateIndex >= rates.Length)
                    nextRateIndex = 0;

                long totalAverageRates = 0;
                for (int i = 0; i < ratesUsed; i++)
                {
                    totalAverageRates += rates[i];
                }
                averageRate = (totalAverageRates / ratesUsed);
            }
        }
        #endregion
    }
}
