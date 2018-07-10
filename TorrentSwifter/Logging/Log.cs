using System;

namespace TorrentSwifter.Logging
{
    /// <summary>
    /// The log to use for writing messages that can be read by the end-user or a developer.
    /// </summary>
    public static class Log
    {
        #region Fields
        private static LogLevel logLevel = LogLevel.Info;
        private static bool logExceptionStacktraces = true;
        private static ILogger logger = new ConsoleLogger();

        private static bool isLoggingUnhandledExceptions = false;

        private static readonly object syncObj = new object();
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
        #region Unhandled Exceptions
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
        #endregion

        #region Log Exception
        /// <summary>
        /// Logs a fatal exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogFatalException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            else if (logLevel < LogLevel.Fatal)
                return;

            string exceptionMessage = (logExceptionStacktraces ? exception.ToString() : exception.Message);
            LogText(LogLevel.Fatal, exceptionMessage);
        }

        /// <summary>
        /// Logs an error exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogErrorException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            else if (logLevel < LogLevel.Error)
                return;

            string exceptionMessage = (logExceptionStacktraces ? exception.ToString() : exception.Message);
            LogText(LogLevel.Error, exceptionMessage);
        }

        /// <summary>
        /// Logs a warning exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogWarningException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            else if (logLevel < LogLevel.Warning)
                return;

            string exceptionMessage = (logExceptionStacktraces ? exception.ToString() : exception.Message);
            LogText(LogLevel.Warning, exceptionMessage);
        }

        /// <summary>
        /// Logs an informational exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogInfoException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            else if (logLevel < LogLevel.Info)
                return;

            string exceptionMessage = (logExceptionStacktraces ? exception.ToString() : exception.Message);
            LogText(LogLevel.Info, exceptionMessage);
        }

        /// <summary>
        /// Logs a debugging exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogDebugException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            else if (logLevel < LogLevel.Debug)
                return;

            string exceptionMessage = (logExceptionStacktraces ? exception.ToString() : exception.Message);
            LogText(LogLevel.Debug, exceptionMessage);
        }
        #endregion

        #region Log Fatal
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
        #endregion

        #region Log Error
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
        #endregion

        #region Log Warning
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
        #endregion

        #region Log Info
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
        #endregion

        #region Log Debug
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
        #endregion

        #region Private Methods
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            bool isFatal = e.IsTerminating;
            var exceptionObj = e.ExceptionObject;
            var exception = exceptionObj as Exception;
            if (exception != null)
            {
                if (isFatal)
                    LogFatalException(exception);
                else
                    LogErrorException(exception);
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
                lock (syncObj)
                {
                    logger.WritePrefixes(messageLevel);
                    logger.WriteLine(messageLevel, text);
                }
            }
        }
        #endregion
    }
}
