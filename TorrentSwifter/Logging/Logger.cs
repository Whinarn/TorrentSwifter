using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// The logger to use for writing log messages that can be read by the end-user or a developer.
    /// </summary>
    public static class Logger
    {
        #region Fields
        private static LogLevel logLevel = LogLevel.Info;
        private static bool logExceptionStacktraces = true;
        private static ILogger logger = new ConsoleLogger();

        private static bool isLoggingUnhandledExceptions = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the logging level that is used for all log messages.
        /// </summary>
        public static LogLevel Level
        {
            get { return logLevel; }
            set { logLevel = value; }
        }

        /// <summary>
        /// Gets or sets if the stacktraces for exceptions are also logged.
        /// </summary>
        public static bool LogExceptionStacktraces
        {
            get { return logExceptionStacktraces; }
            set { logExceptionStacktraces = value; }
        }

        /// <summary>
        /// Gets or sets the logger implementation to use.
        /// </summary>
        public static ILogger Implementation
        {
            get { return logger; }
            set { logger = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts logging all unhandled exceptions.
        /// </summary>
        public static void StartLoggingUnhandledExceptions()
        {
            if (isLoggingUnhandledExceptions)
                return;

            isLoggingUnhandledExceptions = true;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Starts logging all unhandled exceptions.
        /// </summary>
        public static void StopLoggingUnhandledExceptions()
        {
            if (!isLoggingUnhandledExceptions)
                return;

            isLoggingUnhandledExceptions = false;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="isFatal">If the exception is fatal.</param>
        public static void LogException(Exception exception, bool isFatal)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            if (logExceptionStacktraces)
            {
                if (isFatal)
                    LogFatal(exception.ToString());
                else
                    LogError(exception.ToString());
            }
            else
            {
                if (isFatal)
                    LogFatal(exception.Message);
                else
                    LogError(exception.Message);
            }
        }

        /// <summary>
        /// Writes a fatal message.
        /// </summary>
        /// <param name="text">The text message.</param>
        public static void LogFatal(string text)
        {
            LogText(LogLevel.Fatal, text);
        }

        /// <summary>
        /// Writes a fatal message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        public static void LogFatal(string format, object arg0)
        {
            if (logLevel < LogLevel.Fatal)
                return;

            string text = string.Format(format, arg0);
            LogFatal(text);
        }

        /// <summary>
        /// Writes a fatal message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        /// <param name="arg1">The second message format argument.</param>
        public static void LogFatal(string format, object arg0, object arg1)
        {
            if (logLevel < LogLevel.Fatal)
                return;

            string text = string.Format(format, arg0, arg1);
            LogFatal(text);
        }

        /// <summary>
        /// Writes a fatal message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="args">The message format arguments.</param>
        public static void LogFatal(string format, params object[] args)
        {
            if (logLevel < LogLevel.Fatal)
                return;

            string text = string.Format(format, args);
            LogFatal(text);
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="text">The text message.</param>
        public static void LogError(string text)
        {
            LogText(LogLevel.Error, text);
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        public static void LogError(string format, object arg0)
        {
            if (logLevel < LogLevel.Error)
                return;

            string text = string.Format(format, arg0);
            LogError(text);
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        /// <param name="arg1">The second message format argument.</param>
        public static void LogError(string format, object arg0, object arg1)
        {
            if (logLevel < LogLevel.Error)
                return;

            string text = string.Format(format, arg0, arg1);
            LogError(text);
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="args">The message format arguments.</param>
        public static void LogError(string format, params object[] args)
        {
            if (logLevel < LogLevel.Error)
                return;

            string text = string.Format(format, args);
            LogError(text);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="text">The text message.</param>
        public static void LogWarning(string text)
        {
            LogText(LogLevel.Warning, text);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        public static void LogWarning(string format, object arg0)
        {
            if (logLevel < LogLevel.Warning)
                return;

            string text = string.Format(format, arg0);
            LogWarning(text);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        /// <param name="arg1">The second message format argument.</param>
        public static void LogWarning(string format, object arg0, object arg1)
        {
            if (logLevel < LogLevel.Warning)
                return;

            string text = string.Format(format, arg0, arg1);
            LogWarning(text);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="args">The message format arguments.</param>
        public static void LogWarning(string format, params object[] args)
        {
            if (logLevel < LogLevel.Warning)
                return;

            string text = string.Format(format, args);
            LogWarning(text);
        }

        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="text">The text message.</param>
        public static void LogInfo(string text)
        {
            LogText(LogLevel.Info, text);
        }

        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        public static void LogInfo(string format, object arg0)
        {
            if (logLevel < LogLevel.Info)
                return;

            string text = string.Format(format, arg0);
            LogInfo(text);
        }

        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        /// <param name="arg1">The second message format argument.</param>
        public static void LogInfo(string format, object arg0, object arg1)
        {
            if (logLevel < LogLevel.Info)
                return;

            string text = string.Format(format, arg0, arg1);
            LogInfo(text);
        }

        /// <summary>
        /// Writes an informational message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="args">The message format arguments.</param>
        public static void LogInfo(string format, params object[] args)
        {
            if (logLevel < LogLevel.Info)
                return;

            string text = string.Format(format, args);
            LogInfo(text);
        }

        /// <summary>
        /// Writes a debugging message.
        /// </summary>
        /// <param name="text">The text message.</param>
        public static void LogDebug(string text)
        {
            LogText(LogLevel.Debug, text);
        }

        /// <summary>
        /// Writes a debugging message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        public static void LogDebug(string format, object arg0)
        {
            if (logLevel < LogLevel.Debug)
                return;

            string text = string.Format(format, arg0);
            LogDebug(text);
        }

        /// <summary>
        /// Writes a debugging message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="arg0">The first message format argument.</param>
        /// <param name="arg1">The second message format argument.</param>
        public static void LogDebug(string format, object arg0, object arg1)
        {
            if (logLevel < LogLevel.Debug)
                return;

            string text = string.Format(format, arg0, arg1);
            LogDebug(text);
        }

        /// <summary>
        /// Writes a debugging message.
        /// </summary>
        /// <param name="format">The message format string.</param>
        /// <param name="args">The message format arguments.</param>
        public static void LogDebug(string format, params object[] args)
        {
            if (logLevel < LogLevel.Debug)
                return;

            string text = string.Format(format, args);
            LogDebug(text);
        }
        #endregion

        #region Private Methods
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            bool isFatal = e.IsTerminating;
            var exceptionObj = e.ExceptionObject;
            var exception = exceptionObj as Exception;
            if (exception != null)
            {
                LogException(exception, isFatal);
            }
            else if (exceptionObj != null)
            {
                if (isFatal)
                {
                    LogFatal(exceptionObj.ToString());
                }
                else
                {
                    LogError(exceptionObj.ToString());
                }
            }
            else if (isFatal)
            {
                LogFatal("Fatal unhandled exception occured, without any details provided!");
            }
        }

        private static void LogText(LogLevel messageLevel, string text)
        {
            if (logger != null && logLevel >= messageLevel)
            {
                logger.WritePrefixes(messageLevel);
                logger.WriteLine(messageLevel, text);
            }
        }
        #endregion
    }
}
