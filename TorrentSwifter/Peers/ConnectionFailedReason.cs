using System;

namespace TorrentSwifter.Peers
{
    /// <summary>
    /// Reasons for a connection failure.
    /// </summary>
    public enum ConnectionFailedReason
    {
        /// <summary>
        /// Unknown reason.
        /// </summary>
        Unknown,
        /// <summary>
        /// The connection attempt timed out.
        /// </summary>
        TimedOut,
        /// <summary>
        /// Failed to resolve the hostname.
        /// </summary>
        Resolve,
    }
}
