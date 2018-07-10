using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Connection failed attempt event arguments.
    /// </summary>
    public sealed class ConnectionFailedEventArgs : EventArgs
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
        public ConnectionFailedEventArgs(ConnectionFailedReason failedReason)
        {
            this.failedReason = failedReason;
        }
    }
}
