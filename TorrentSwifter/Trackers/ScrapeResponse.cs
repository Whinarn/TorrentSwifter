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
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// A response to a tracker scrape request.
    /// </summary>
    public sealed class ScrapeResponse
    {
        #region Classes
        /// <summary>
        /// Torrent information received by a scrape response.
        /// </summary>
        public struct TorrentInfo
        {
            private readonly InfoHash infoHash;
            private readonly int completeCount;
            private readonly int incompleteCount;
            private readonly int downloadedCount;
            private readonly string name;

            /// <summary>
            /// Gets the torrent info hash.
            /// </summary>
            public InfoHash InfoHash
            {
                get { return infoHash; }
            }

            /// <summary>
            /// Gets the count of completed peers (aka seeders) of the torrent.
            /// </summary>
            public int CompleteCount
            {
                get { return completeCount; }
            }

            /// <summary>
            /// Gets the count of incompleted peers (aka leechers) of the torrent.
            /// </summary>
            public int IncompleteCount
            {
                get { return incompleteCount; }
            }

            /// <summary>
            /// Gets the count of completed downloads of the torrent.
            /// </summary>
            public int DownloadedCount
            {
                get { return downloadedCount; }
            }

            /// <summary>
            /// Gets the name of the torrent.
            /// Note that this can be null if the tracker did not send it.
            /// </summary>
            public string Name
            {
                get { return name; }
            }

            /// <summary>
            /// Creates new torrent information.
            /// </summary>
            /// <param name="infoHash">The torrent info hash.</param>
            /// <param name="completeCount">The complete peer count.</param>
            /// <param name="incompleteCount">The incomplete peer count.</param>
            /// <param name="downloadedCount">The count of completed downloads.</param>
            /// <param name="name">The torrent name.</param>
            public TorrentInfo(InfoHash infoHash, int completeCount, int incompleteCount, int downloadedCount, string name)
            {
                this.infoHash = infoHash;
                this.completeCount = completeCount;
                this.incompleteCount = incompleteCount;
                this.downloadedCount = downloadedCount;
                this.name = name;
            }
        }
        #endregion

        #region Fields
        private readonly Tracker tracker;
        private readonly TorrentInfo[] torrents;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the tracker that sent the response.
        /// </summary>
        public Tracker Tracker
        {
            get { return tracker; }
        }

        /// <summary>
        /// Gets the torrents returned from the tracker.
        /// </summary>
        public TorrentInfo[] Torrents
        {
            get { return torrents; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new scrape response.
        /// </summary>
        /// <param name="tracker">The tracker that sent the response.</param>
        /// <param name="torrents">The torrents.</param>
        public ScrapeResponse(Tracker tracker, TorrentInfo[] torrents)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");
            else if (torrents == null)
                throw new ArgumentNullException("torrents");

            this.tracker = tracker;
            this.torrents = torrents;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to retrieve torrent information from an info hash.
        /// </summary>
        /// <param name="infoHash">The torrent info hash.</param>
        /// <param name="torrentInfo">The output torrent information.</param>
        /// <returns>If found.</returns>
        public bool TryGetTorrentInfo(InfoHash infoHash, out TorrentInfo torrentInfo)
        {
            torrentInfo = default(TorrentInfo);
            if (torrents == null)
                return false;

            bool result = false;

            for (int i = 0; i < torrents.Length; i++)
            {
                if (torrents[i].InfoHash.Equals(infoHash))
                {
                    torrentInfo = torrents[i];
                    result = true;
                    break;
                }
            }

            return result;
        }
        #endregion
    }
}
