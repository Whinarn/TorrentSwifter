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
        /// The connection attempt was aborted.
        /// </summary>
        Aborted,
        /// <summary>
        /// The connection attempt timed out.
        /// </summary>
        TimedOut,
        /// <summary>
        /// Failed to resolve the hostname.
        /// </summary>
        NameResolve,
        /// <summary>
        /// The host is down.
        /// </summary>
        HostDown,
        /// <summary>
        /// The host is unreachable.
        /// </summary>
        HostUnreachable,
        /// <summary>
        /// The connection attempt was refused by the host.
        /// </summary>
        Refused,
        /// <summary>
        /// There is no internet connection available.
        /// </summary>
        NoInternetConnection,
        /// <summary>
        /// The address family, protocol or socket type is not supported.
        /// </summary>
        NotSupported,
        /// <summary>
        /// The process does not have access to perform connections.
        /// </summary>
        AccessDenied
    }
}
