using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// A group of loggers as one logger.
    /// </summary>
    public sealed class GroupLogger : ILogger
    {
        private ILogger[] loggers = null;

        /// <summary>
        /// Gets the loggers of this group.
        /// </summary>
        public ILogger[] Loggers
        {
            get { return loggers; }
        }

        /// <summary>
        /// Creates a new file logger.
        /// </summary>
        /// <param name="loggers">The loggers to group together.</param>
        public GroupLogger(ILogger[] loggers)
        {
            if (loggers == null)
                throw new ArgumentNullException("loggers");

            for (int i = 0; i < loggers.Length; i++)
            {
                if (loggers[i] == null)
                    throw new ArgumentException(string.Format("The logger at index {0} is null.", i), "loggers");
            }

            this.loggers = loggers;
        }

        /// <summary>
        /// The finalizer.
        /// </summary>
        ~GroupLogger()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of this file logger.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (loggers != null)
            {
                for (int i = 0; i < loggers.Length; i++)
                {
                    loggers[i].Dispose();
                }
                loggers = null;
            }
        }

        /// <summary>
        /// Writes the log prefixes ahead of a message.
        /// </summary>
        /// <param name="level">The log message level.</param>
        public void WritePrefixes(LogLevel level)
        {
            for (int i = 0; i < loggers.Length; i++)
            {
                loggers[i].WritePrefixes(level);
            }
        }

        /// <summary>
        /// Writes a log message to this logger.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public void Write(LogLevel level, string message)
        {
            for (int i = 0; i < loggers.Length; i++)
            {
                loggers[i].Write(level, message);
            }
        }

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public void WriteLine(LogLevel level, string message)
        {
            for (int i = 0; i < loggers.Length; i++)
            {
                loggers[i].WriteLine(level, message);
            }
        }
    }
}
