using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// The available log levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// All logging is off.
        /// </summary>
        Off = 0,
        /// <summary>
        /// Fatal messages.
        /// </summary>
        Fatal = 1,
        /// <summary>
        /// Error messages.
        /// </summary>
        Error = 2,
        /// <summary>
        /// Warning messages.
        /// </summary>
        Warning = 3,
        /// <summary>
        /// Informational messages.
        /// </summary>
        Info = 4,
        /// <summary>
        /// Debugging messages.
        /// </summary>
        Debug = 5
    }
}
