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
