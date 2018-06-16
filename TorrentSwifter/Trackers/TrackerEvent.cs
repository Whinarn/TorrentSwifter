using System;

namespace TorrentSwifter.Trackers
{
    // NOTE: The value of the tracker events represent the values used for UDP trackers, so be careful if you change them.

    /// <summary>
    /// A tracker event.
    /// </summary>
    public enum TrackerEvent : int
    {
        /// <summary>
        /// No specific event.
        /// </summary>
        None = 0,
        /// <summary>
        /// Completed download event.
        /// </summary>
        Completed = 1,
        /// <summary>
        /// Start event.
        /// </summary>
        Started = 2,
        /// <summary>
        /// Stopped event.
        /// </summary>
        Stopped = 3
    }
}
