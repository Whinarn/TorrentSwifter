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
using System.Threading.Tasks;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// A torrent tracker.
    /// </summary>
    public abstract class Tracker : IDisposable
    {
        #region Fields
        private readonly Uri uri;

        /// <summary>
        /// The count of completed peers (aka seeders) for this tracker.
        /// </summary>
        protected int completeCount = 0;
        /// <summary>
        /// The count of incompleted peers (aka leechers) for this tracker.
        /// </summary>
        protected int incompleteCount = 0;
        /// <summary>
        /// The count of completed downloads reported by the tracker.
        /// </summary>
        protected int downloadedCount = 0;

        /// <summary>
        /// The minimum interval between updates to this tracker.
        /// </summary>
        protected TimeSpan minInterval = TimeSpan.FromMinutes(3.0);
        /// <summary>
        /// The interval between updates to this tracker.
        /// </summary>
        protected TimeSpan interval = TimeSpan.FromMinutes(30.0);

        /// <summary>
        /// The tracker status.
        /// </summary>
        protected TrackerStatus status = TrackerStatus.OK;

        /// <summary>
        /// The failure message of this tracker.
        /// </summary>
        protected string failureMessage = null;
        /// <summary>
        /// The  warning message of this tracker.
        /// </summary>
        protected string warningMessage = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the URI of this tracker.
        /// </summary>
        public Uri Uri
        {
            get { return uri; }
        }

        /// <summary>
        /// Gets the count of completed peers (aka seeders) for this tracker.
        /// </summary>
        public int CompleteCount
        {
            get { return completeCount; }
        }

        /// <summary>
        /// Gets the count of incompleted peers (aka leechers) for this tracker.
        /// </summary>
        public int IncompleteCount
        {
            get { return incompleteCount; }
        }

        /// <summary>
        /// Gets the count of completed downloads reported by the tracker.
        /// </summary>
        public int DownloadedCount
        {
            get { return downloadedCount; }
        }

        /// <summary>
        /// Gets or sets the interval between updates of this tracker.
        /// </summary>
        public TimeSpan Interval
        {
            get { return interval; }
            set { interval = (value >= minInterval ? value : minInterval); }
        }

        /// <summary>
        /// Gets the minimum interval between updates of this tracker.
        /// </summary>
        public TimeSpan MinInterval
        {
            get { return minInterval; }
        }

        /// <summary>
        /// Gets the status of the tracker.
        /// </summary>
        public TrackerStatus Status
        {
            get { return status; }
        }

        /// <summary>
        /// Gets the failure message of this tracker.
        /// </summary>
        public string FailureMessage
        {
            get { return failureMessage ?? string.Empty; }
        }

        /// <summary>
        /// Gets the warning message of this tracker.
        /// </summary>
        public string WarningMessage
        {
            get { return warningMessage ?? string.Empty; }
        }

        /// <summary>
        /// Gets if we can announce to this tracker.
        /// </summary>
        public abstract bool CanAnnounce
        {
            get;
        }

        /// <summary>
        /// Gets if this tracker can be scraped.
        /// </summary>
        public abstract bool CanScrape
        {
            get;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new tracker.
        /// </summary>
        /// <param name="uri">The tracker URI.</param>
        public Tracker(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            else if (!uri.IsAbsoluteUri)
                throw new ArgumentException("The provided URI must be absolute.", "uri");

            this.uri = uri;
        }
        #endregion

        #region Finalizer
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Tracker()
        {
            Dispose(false);
        }
        #endregion

        #region Disposing
        /// <summary>
        /// Disposes of this tracker.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Called when this tracker is being disposed of.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected abstract void Dispose(bool disposing);
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Makes an announce request to this tracker.
        /// </summary>
        /// <param name="request">The announce request object.</param>
        /// <returns>The announce response.</returns>
        public abstract Task<AnnounceResponse> Announce(AnnounceRequest request);

        /// <summary>
        /// Makes a scrape request to this tracker.
        /// </summary>
        /// <param name="infoHashes">The optional array of info hashes. Can be null or empty.</param>
        /// <returns>The announce response.</returns>
        public abstract Task<ScrapeResponse> Scrape(InfoHash[] infoHashes);
        #endregion

        #region Create
        /// <summary>
        /// Creates a tracker from an URI.
        /// </summary>
        /// <param name="uri">The tracker URI.</param>
        /// <returns>The created tracker, if any.</returns>
        public static Tracker Create(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            else if (!uri.IsAbsoluteUri)
                throw new ArgumentException("The URI must be absolute.", "uri");

            string uriScheme = uri.Scheme;
            if (string.Equals(uriScheme, "http", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uriScheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpTracker(uri);
            }
            else if (string.Equals(uriScheme, "udp", StringComparison.OrdinalIgnoreCase))
            {
                return new UdpTracker(uri);
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}
