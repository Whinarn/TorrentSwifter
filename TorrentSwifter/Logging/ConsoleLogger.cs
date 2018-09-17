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
    /// A console logger.
    /// </summary>
    public sealed class ConsoleLogger : LoggerBase
    {
        private static readonly ConsoleColor[] LevelColors =
            { ConsoleColor.White, ConsoleColor.DarkRed, ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.White, ConsoleColor.Gray };
        //    Off,                Fatal,                Error,            Warning,             Info,               Debug

        private bool useColors = true;
        private ConsoleColor defaultColor = ConsoleColor.White;

        /// <summary>
        /// Gets or sets if the console logger uses different colors depending on the log level.
        /// </summary>
        public bool UseColors
        {
            get { return useColors; }
            set { useColors = value; }
        }

        /// <summary>
        /// Creates a new console logger.
        /// </summary>
        public ConsoleLogger()
            : this(true)
        {
#if DEBUG
            // We use debug logging by default for debug builds
            base.level = LogLevel.Debug;
#endif
        }

        /// <summary>
        /// Creates a new console logger.
        /// </summary>
        /// <param name="useColors">If this logger should use different colors depending on the log level.</param>
        public ConsoleLogger(bool useColors)
        {
            this.useColors = useColors;
            if (useColors)
            {
                try
                {
                    defaultColor = Console.ForegroundColor;
                }
                catch
                {
                    useColors = false;
                    defaultColor = ConsoleColor.White;
                }
            }
        }

        /// <summary>
        /// Writes a log message to this logger.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public override void Write(LogLevel level, string message)
        {
            if (useColors)
            {
                var levelColor = LevelColors[(int)level % LevelColors.Length];
                Console.ForegroundColor = levelColor;
            }
            if (level >= LogLevel.Error)
            {
                Console.Error.Write(message);
            }
            else
            {
                Console.Write(message);
            }
            if (useColors)
            {
                Console.ForegroundColor = defaultColor;
            }
        }

        /// <summary>
        /// Writes a log message to this logger including a trailing line-ending.
        /// </summary>
        /// <param name="level">The log message level.</param>
        /// <param name="message">The log message text.</param>
        public override void WriteLine(LogLevel level, string message)
        {
            if (useColors)
            {
                var levelColor = LevelColors[(int)level % LevelColors.Length];
                Console.ForegroundColor = levelColor;
            }
            if (level >= LogLevel.Error)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (useColors)
            {
                Console.ForegroundColor = defaultColor;
            }
        }
    }
}
