using System;

namespace TorrentSwifter.Trackers
{
    /// <summary>
    /// Tracker statuses.
    /// </summary>
    public enum TrackerStatus
    {
        /// <summary>
        /// The tracker is okay.
        /// </summary>
        OK,
        /// <summary>
        /// The tracker is offline.
        /// </summary>
        Offline,
        /// <summary>
        /// The tracker is giving invalid responses.
        /// </summary>
        InvalidResponse
    }
}
