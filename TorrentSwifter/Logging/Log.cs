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
    /// The log to use for writing messages that can be read by the end-user or a developer.
    /// </summary>
    public static class Log
    {
        #region Consts
        private const string ExceptionLogPrefix = "[EXCEPTION] ";
        #endregion

        #region Fields
        private static bool logExceptionStacktraces = true;
        private static ILogger logger = defaultLogger;

        private static bool isLoggingUnhandledExceptions = false;

        private static readonly object syncObj = new object();

        private static readonly ILogger defaultLogger = new ConsoleLogger();
        #endregion

        #region Properties
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
        public static ILogger Logger
        {
            get { return logger; }
            set { logger = value ?? defaultLogger; }
        }
        #endregion

        #region Static Initializer
        static Log()
        {
            logger = defaultLogger;
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
            else if (logger.Level < LogLevel.Fatal)
                return;

            string exceptionMessage = ExceptionLogPrefix + (logExceptionStacktraces ? exception.ToString() : exception.Message);
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
            else if (logger.Level < LogLevel.Error)
                return;

            string exceptionMessage = ExceptionLogPrefix + (logExceptionStacktraces ? exception.ToString() : exception.Message);
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
            else if (logger.Level < LogLevel.Warning)
                return;

            string exceptionMessage = ExceptionLogPrefix + (logExceptionStacktraces ? exception.ToString() : exception.Message);
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
            else if (logger.Level < LogLevel.Info)
                return;

            string exceptionMessage = ExceptionLogPrefix + (logExceptionStacktraces ? exception.ToString() : exception.Message);
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
            else if (logger.Level < LogLevel.Debug)
                return;

            string exceptionMessage = ExceptionLogPrefix + (logExceptionStacktraces ? exception.ToString() : exception.Message);
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
            if (logger.Level < LogLevel.Fatal)
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
            if (logger.Level < LogLevel.Fatal)
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
            if (logger.Level < LogLevel.Fatal)
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
            if (logger.Level < LogLevel.Error)
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
            if (logger.Level < LogLevel.Error)
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
            if (logger.Level < LogLevel.Error)
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
            if (logger.Level < LogLevel.Warning)
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
            if (logger.Level < LogLevel.Warning)
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
            if (logger.Level < LogLevel.Warning)
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
            if (logger.Level < LogLevel.Info)
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
            if (logger.Level < LogLevel.Info)
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
            if (logger.Level < LogLevel.Info)
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
            if (logger.Level < LogLevel.Debug)
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
            if (logger.Level < LogLevel.Debug)
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
            if (logger.Level < LogLevel.Debug)
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
            if (logger != null && logger.Level >= messageLevel)
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
