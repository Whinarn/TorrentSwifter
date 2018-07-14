using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Peer connection failed attempt event arguments.
    /// </summary>
    public sealed class PeerConnectionFailedEventArgs : EventArgs
    {
        private readonly ConnectionFailedReason failedReason;

        /// <summary>
        /// Gets the reason of failure for the connection attempt.
        /// </summary>
        public ConnectionFailedReason FailedReason
        {
            get { return failedReason; }
        }

        /// <summary>
        /// Creates new connection failed attempt event arguments.
        /// </summary>
        /// <param name="failedReason">The reason of failure.</param>
        public PeerConnectionFailedEventArgs(ConnectionFailedReason failedReason)
        {
            this.failedReason = failedReason;
        }
    }

    /// <summary>
    /// Peer connection state event arguments.
    /// </summary>
    public sealed class PeerConnectionStateEventArgs : EventArgs
    {
        private readonly bool isInterested;
        private readonly bool isChoked;

        /// <summary>
        /// Gets if the peer is interested.
        /// </summary>
        public bool IsInterested
        {
            get { return isInterested; }
        }

        /// <summary>
        /// Gets if the peer is choking us.
        /// </summary>
        public bool IsChoked
        {
            get { return isChoked; }
        }

        /// <summary>
        /// Creates new connection state event arguments.
        /// </summary>
        /// <param name="isInterested">If the peer is interested.</param>
        /// <param name="isChoked">If the peer is choking us.</param>
        public PeerConnectionStateEventArgs(bool isInterested, bool isChoked)
        {
            this.isInterested = isInterested;
            this.isChoked = isChoked;
        }
    }
}
