using System;
using System.IO;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// A file logger.
    /// </summary>
    public sealed class FileLogger : LoggerBase
    {
        private StreamWriter streamWriter = null;

        /// <summary>
        /// Creates a new file logger.
        /// </summary>
        /// <param name="filePath">The path to write the log file to.</param>
        public FileLogger(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            base.prependTimestamp = true;
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
            streamWriter.Write(message);
        }

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public override void WriteLine(LogLevel level, string message)
        {
            streamWriter.WriteLine(message);
        }
    }
}
