using System;
using System.Collections;
using System.Collections.Generic;
using TorrentSwifter.Peers;
using TorrentSwifter.Torrents;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// A group of trackers.
    /// </summary>
    public sealed class TrackerGroup : IEnumerable<Tracker>, IEnumerable
    {
        #region Fields
        private readonly Torrent torrent;

        private List<Tracker> trackers = new List<Tracker>();

        private AnnounceRequest announceRequest = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the count of trackers in this group.
        /// </summary>
        public int TrackerCount
        {
            get { return trackers.Count; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new tracker group.
        /// </summary>
        /// <param name="torrent">The parent torrent.</param>
        public TrackerGroup(Torrent torrent)
        {
            this.torrent = torrent;

            int listenPort = PeerListener.Port;
            announceRequest = new AnnounceRequest(torrent.InfoHash, torrent.PeerID, listenPort);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a tracker to this group.
        /// </summary>
        /// <param name="tracker">The tracker to add.</param>
        public void AddTracker(Tracker tracker)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            if (!trackers.Contains(tracker))
            {
                trackers.Add(tracker);
            }
        }

        /// <summary>
        /// Returns if a specific tracker is contained in this group.
        /// </summary>
        /// <param name="tracker">The tracker.</param>
        /// <returns>If the group contains the tracker.</returns>
        public bool HasTracker(Tracker tracker)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            return trackers.Contains(tracker);
        }

        /// <summary>
        /// Removes a tracker from this group.
        /// </summary>
        /// <param name="tracker">The tracker to remove.</param>
        /// <returns>If successfully removed.</returns>
        public bool RemoveTracker(Tracker tracker)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            return trackers.Remove(tracker);
        }

        /// <summary>
        /// Removes a tracker from this group by its index.
        /// </summary>
        /// <param name="index">The tracker index.</param>
        public void RemoveTrackerAt(int index)
        {
            if (index < 0 || index >= trackers.Count)
                throw new ArgumentOutOfRangeException("index");

            trackers.RemoveAt(index);
        }

        /// <summary>
        /// Gets the tracker at a specific index.
        /// </summary>
        /// <param name="index">The tracker index.</param>
        /// <returns>The tracker.</returns>
        public Tracker GetTracker(int index)
        {
            if (index < 0 || index >= trackers.Count)
                throw new ArgumentOutOfRangeException("index");

            return trackers[index];
        }

        /// <summary>
        /// Shuffles the trackers in this group.
        /// </summary>
        public void Shuffle()
        {
            if (trackers.Count <= 1)
                return;

            // By specification, it should be random
            var random = new Random();
            trackers.Sort((x, y) => random.Next().CompareTo(random.Next()));
        }

        /// <summary>
        /// Returns the enumerator for this tracker group.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<Tracker> GetEnumerator()
        {
            return trackers.GetEnumerator();
        }
        #endregion

        #region Internal Methods
        internal void Update()
        {
            if (trackers.Count == 0)
                return;

            // TODO: Implement!
        }
        #endregion

        #region Private Methods
        IEnumerator IEnumerable.GetEnumerator()
        {
            return trackers.GetEnumerator();
        }
        #endregion
    }
}
