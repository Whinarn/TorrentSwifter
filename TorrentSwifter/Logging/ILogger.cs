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
    /// An implementation for a logger.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Gets or sets the logging level for this logger.
        /// </summary>
        LogLevel Level { get; set; }

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
