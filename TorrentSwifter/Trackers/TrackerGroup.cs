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
using System.Threading.Tasks;
using TorrentSwifter.Helpers;
using TorrentSwifter.Logging;
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
        private int currentIndex = 0;

        private bool hasSentStartedEvent = false;
        private bool isAnnouncing = false;
        private DateTime nextAnnounceTime = DateTime.MinValue;
        private DateTime nextAnnounceTimeMinimum = DateTime.MinValue;

        private TrackerStatus status = TrackerStatus.Offline;
        private string failureMessage = null;
        private string warningMessage = null;

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

        /// <summary>
        /// Gets if we are currently announcing.
        /// </summary>
        public bool IsAnnouncing
        {
            get { return isAnnouncing; }
        }

        /// <summary>
        /// Gets the status of this group of trackers.
        /// </summary>
        public TrackerStatus Status
        {
            get { return status; }
        }

        /// <summary>
        /// Gets the failure message for this group of trackers.
        /// </summary>
        public string FailureMessage
        {
            get { return failureMessage ?? string.Empty; }
        }

        /// <summary>
        /// Gets the warning message for this group of trackers.
        /// </summary>
        public string WarningMessage
        {
            get { return warningMessage ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets the next time we should announce.
        /// </summary>
        public DateTime NextAnnounceTime
        {
            get { return nextAnnounceTime; }
            set
            {
                if (value > nextAnnounceTimeMinimum)
                    nextAnnounceTime = value;
                else
                    nextAnnounceTime = nextAnnounceTimeMinimum;
            }
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
            RandomHelper.Randomize(trackers);
        }

        /// <summary>
        /// Announces to this tracker group.
        /// This should be done in regular intervals.
        /// </summary>
        /// <returns>The announce response.</returns>
        public async Task<AnnounceResponse> Announce(TrackerEvent trackerEvent)
        {
            // If there are no trackers to announce to
            if (trackers.Count == 0)
                return null;
            // Don't bother announcing if we are not listening for peers
            else if (!PeerListener.IsListening)
                return null;
            else if (isAnnouncing)
                return null;

            try
            {
                if (currentIndex > trackers.Count)
                    currentIndex = 0;

                // Select the tracker
                var tracker = trackers[currentIndex];
                isAnnouncing = true;

                // If we haven't sent started event yet, we make sure to send started event first
                if (!hasSentStartedEvent)
                {
                    // If we are sending stopped and haven't sent started, then we don't have to bother
                    if (trackerEvent == TrackerEvent.Stopped)
                        return null;

                    trackerEvent = TrackerEvent.Started;
                }

                // If we stop the tracker, we have to send a started event again on next announce
                if (trackerEvent == TrackerEvent.Stopped)
                {
                    hasSentStartedEvent = false;
                }

                int listenPort = PeerListener.Port;
                announceRequest.Port = listenPort;
                announceRequest.TrackerEvent = trackerEvent;
                announceRequest.BytesDownloaded = torrent.SessionDownloadedBytes;
                announceRequest.BytesUploaded = torrent.SessionUploadedBytes;
                announceRequest.BytesLeft = torrent.BytesLeftToDownload;

                var announceResponse = await tracker.Announce(announceRequest);
                status = tracker.Status;
                failureMessage = tracker.FailureMessage;
                warningMessage = tracker.WarningMessage;

                if (status == TrackerStatus.OK)
                {
                    var timeNow = DateTime.UtcNow;
                    nextAnnounceTime = timeNow.Add(tracker.Interval);
                    nextAnnounceTimeMinimum = timeNow.Add(tracker.MinInterval);

                    if (!hasSentStartedEvent && trackerEvent == TrackerEvent.Started)
                    {
                        hasSentStartedEvent = true;
                    }

                    int trackerIndex = trackers.IndexOf(tracker);
                    if (trackerIndex != -1)
                    {
                        // If we get an OK we put this tracker first in the list
                        if (trackerIndex > 0)
                        {
                            trackers.RemoveAt(trackerIndex);
                            trackers.Insert(0, tracker);
                        }
                        currentIndex = 0;
                    }
                    else
                    {
                        // The tracker has been removed from the group
                        currentIndex = 0;
                    }
                }
                else
                {
                    currentIndex = (currentIndex + 1) % trackers.Count;

                    if (currentIndex == 0)
                    {
                        var timeNow = DateTime.UtcNow;
                        nextAnnounceTime = timeNow.Add(TimeSpan.FromMinutes(5.0));
                        nextAnnounceTimeMinimum = timeNow.Add(TimeSpan.FromMinutes(1.0));
                    }
                }
                return announceResponse;
            }
            catch (Exception ex)
            {
                failureMessage = ex.Message;
                currentIndex = (currentIndex + 1) % trackers.Count;
                Log.LogErrorException(ex);
                return null;
            }
            finally
            {
                isAnnouncing = false;
            }
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

            if (DateTime.UtcNow >= nextAnnounceTime)
            {
                var announceTask = Announce(TrackerEvent.None);
                announceTask.ContinueWith((task) =>
                {
                    if (!task.IsFaulted)
                    {
                        var announceResponse = task.Result;
                        if (announceResponse != null)
                        {
                            torrent.ProcessAnnounceResponse(announceResponse);
                        }
                    }
                    else
                    {
                        Log.LogErrorException(task.Exception);
                    }
                });
            }
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
