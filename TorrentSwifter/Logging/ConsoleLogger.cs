﻿using System;

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
                var levelColor = LevelColors[(int)level & LevelColors.Length];
                Console.ForegroundColor = levelColor;
            }
            Console.Write(message);
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
                var levelColor = LevelColors[(int)level & LevelColors.Length];
                Console.ForegroundColor = levelColor;
            }
            Console.WriteLine(message);
            if (useColors)
            {
                Console.ForegroundColor = defaultColor;
            }
        }
    }
}
