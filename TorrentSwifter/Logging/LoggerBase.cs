#region License
/*
MIT License

Copyright (c) 2018 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

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
        /// The desired log level for this logger.
        /// </summary>
        protected LogLevel level = LogLevel.Info;
        /// <summary>
        /// If timestamps should be prepended to all log messages.
        /// </summary>
        protected bool prependTimestamp = false;
        /// <summary>
        /// If log levels should be prepended to all log messages.
        /// </summary>
        protected bool prependLevel = true;

        /// <summary>
        /// Gets or sets the logging level for this logger.
        /// </summary>
        public LogLevel Level
        {
            get { return level; }
            set { level = value; }
        }

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
                string prefixText = LevelPrefixes[(int)level % LevelPrefixes.Length];
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
