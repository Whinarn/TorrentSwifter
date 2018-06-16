using System;
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
        private readonly string failureReason;
        private readonly string warningMessage;
        private readonly PeerInfo[] peers;
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
    }
}
