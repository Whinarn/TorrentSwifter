using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// A logger base that includes some basic functionality for loggers.
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        private static readonly string[] LevelPrefixes = { "", "[FATAL] ", "[ERROR] ", "[WARNING] ", "[INFO] ", "[DEBUG] " };

        /// <summary>
        /// If timestamps should be prepended to all log messages.
        /// </summary>
        protected bool prependTimestamp = false;
        /// <summary>
        /// If log levels should be prepended to all log messages.
        /// </summary>
        protected bool prependLevel = true;

        /// <summary>
        /// Gets if timestamps should be prepended to all log messages.
        /// </summary>
        public bool PrependTimestamp
        {
            get { return prependTimestamp; }
            set { prependTimestamp = value; }
        }

        /// <summary>
        /// Gets if log levels should be prepended to all log messages.
        /// </summary>
        public bool PrependLevel
        {
            get { return prependLevel; }
            set { prependLevel = value; }
        }

        /// <summary>
        /// Creates a new logger base.
        /// </summary>
        public LoggerBase()
        {

        }

        /// <summary>
        /// The finalizer.
        /// </summary>
        ~LoggerBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of this logger.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of this logger.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {

        }

        /// <summary>
        /// Writes the log prefixes ahead of a message.
        /// </summary>
        /// <param name="level">The log message level.</param>
        public virtual void WritePrefixes(LogLevel level)
        {
            if (prependTimestamp)
            {
                var date = DateTime.Now;
                string datePrefixText = string.Format("[{0}] ", date.ToString("G"));
                Write(level, datePrefixText);
            }

            if (prependLevel)
            {
                string prefixText = LevelPrefixes[(int)level & LevelPrefixes.Length];
                Write(level, prefixText);
            }
        }

        /// <summary>
        /// Writes a log message to this logger.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public abstract void Write(LogLevel level, string message);

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public abstract void WriteLine(LogLevel level, string message);
    }
}
