using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// An implementation for a logger.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Writes the log prefixes ahead of a message.
        /// </summary>
        /// <param name="level">The log message level.</param>
        void WritePrefixes(LogLevel level);

        /// <summary>
        /// Writes a log message to this logger.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        void Write(LogLevel level, string message);

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        void WriteLine(LogLevel level, string message);
    }
}
