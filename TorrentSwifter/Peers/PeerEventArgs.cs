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
}
