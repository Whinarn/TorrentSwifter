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
using System.Collections.Generic;
using TorrentSwifter.Peers;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// A response to a tracker announce request.
    /// </summary>
    public sealed class AnnounceResponse
    {
        #region Fields
        private readonly Tracker tracker;
        private string failureReason;
        private string warningMessage;
        private PeerInfo[] peers;
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
        /// Gets the failure reason sent by the tracker, if any.
        /// </summary>
        public string FailureReason
        {
            get { return failureReason; }
        }

        /// <summary>
        /// Gets the warning message sent by the tracker, if any.
        /// </summary>
        public string WarningMessage
        {
            get { return warningMessage; }
        }

        /// <summary>
        /// Gets the peers returned from the tracker.
        /// Note that this can be null.
        /// </summary>
        public PeerInfo[] Peers
        {
            get { return peers; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new announce response.
        /// </summary>
        /// <param name="tracker">The tracker that sent the response.</param>
        /// <param name="failureReason">The failure reason sent by the tracker, if any.</param>
        /// <param name="warningMessage">The warning message sent by the tracker, if any.</param>
        /// <param name="peers">The peers.</param>
        public AnnounceResponse(Tracker tracker, string failureReason, string warningMessage, PeerInfo[] peers)
        {
            if (tracker == null)
                throw new ArgumentNullException("tracker");

            this.tracker = tracker;
            this.failureReason = failureReason;
            this.warningMessage = warningMessage;
            this.peers = peers;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Merges this response with another response.
        /// </summary>
        /// <param name="other">The other response.</param>
        public void Merge(AnnounceResponse other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            else if (other.tracker != tracker)
                throw new ArgumentException("The responses are not from the same tracker.", "other");

            if (string.IsNullOrEmpty(failureReason))
            {
                failureReason = other.failureReason;
            }
            if (string.IsNullOrEmpty(warningMessage))
            {
                warningMessage = other.warningMessage;
            }

            if (peers != null && peers.Length > 0 && other.peers != null && other.peers.Length > 0)
            {
                var peerList = new List<PeerInfo>(peers.Length + other.peers.Length);
                peerList.AddRange(peers);

                for (int i = 0; i < other.peers.Length; i++)
                {
                    if (!peerList.Contains(other.peers[i]))
                    {
                        peerList.Add(other.peers[i]);
                    }
                }

                peers = peerList.ToArray();
            }
            else
            {
                peers = other.peers;
            }
        }
        #endregion
    }
}
