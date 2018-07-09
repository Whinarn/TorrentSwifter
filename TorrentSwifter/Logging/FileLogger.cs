using System;
using System.IO;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// A file logger.
    /// </summary>
    public sealed class FileLogger : LoggerBase
    {
        private LogLevel logLevel = LogLevel.Info;
        private StreamWriter streamWriter = null;

        /// <summary>
        /// Gets or sets the logging level that is used for this file logger.
        /// </summary>
        public LogLevel Level
        {
            get { return logLevel; }
            set { logLevel = value; }
        }

        /// <summary>
        /// Creates a new file logger.
        /// </summary>
        /// <param name="filePath">The path to write the log file to.</param>
        public FileLogger(string filePath)
            : this(filePath, LogLevel.Info)
        {

        }

        /// <summary>
        /// Creates a new file logger.
        /// </summary>
        /// <param name="filePath">The path to write the log file to.</param>
        /// <param name="level"></param>
        public FileLogger(string filePath, LogLevel level)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            base.prependTimestamp = true;
            this.logLevel = level;
            streamWriter = File.CreateText(filePath);
        }

        /// <summary>
        /// Disposes of this logger.
        /// </summary>
        /// <param name="disposing">If disposing, otherwise finalizing.</param>
        protected override void Dispose(bool disposing)
        {
            if (streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }

        /// <summary>
        /// Writes a log message to this logger.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public override void Write(LogLevel level, string message)
        {
            if (logLevel >= level)
            {
                streamWriter.Write(message);
            }
        }

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public override void WriteLine(LogLevel level, string message)
        {
            if (logLevel >= level)
            {
                streamWriter.WriteLine(message);
            }
        }
    }
}
