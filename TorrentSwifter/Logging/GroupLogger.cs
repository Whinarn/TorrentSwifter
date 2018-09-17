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
    /// A group of loggers as one logger.
    /// </summary>
    public sealed class GroupLogger : ILogger
    {
        private ILogger[] loggers = null;

        /// <summary>
        /// Gets or sets the logging level for this logger.
        /// </summary>
        public LogLevel Level
        {
            get
            {
                var maximumLevel = LogLevel.Fatal;
                for (int i = 0; i < loggers.Length; i++)
                {
                    var loggerLevel = loggers[i].Level;
                    if (loggerLevel > maximumLevel)
                    {
                        maximumLevel = loggerLevel;
                    }
                }
                return maximumLevel;
            }
            set { } // Setting the logging level for a group logger isn't supported and doesn't make sense
        }

        /// <summary>
        /// Gets or sets the loggers of this group.
        /// </summary>
        public ILogger[] Loggers
        {
            get { return loggers; }
            set { loggers = value; }
        }

        /// <summary>
        /// Creates a new file logger.
        /// </summary>
        /// <param name="loggers">The loggers to group together.</param>
        public GroupLogger(params ILogger[] loggers)
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
                var loggerLevel = loggers[i].Level;
                if (loggerLevel >= level)
                {
                    loggers[i].WritePrefixes(level);
                }
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
                var loggerLevel = loggers[i].Level;
                if (loggerLevel >= level)
                {
                    loggers[i].Write(level, message);
                }
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
                var loggerLevel = loggers[i].Level;
                if (loggerLevel >= level)
                {
                    loggers[i].WriteLine(level, message);
                }
            }
        }
    }
}
