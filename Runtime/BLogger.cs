#nullable enable
using UnityEngine;
using com.DvosTools.blogger.Service;

namespace com.DvosTools.blogger 
{
    /// <summary>
    /// Static API for BLogger - provides simple logging methods that delegate to BLoggerService
    /// Matches Unity's Debug.Log() signature for easy migration
    /// </summary>
    public static class BLogger
    {
        /// <summary>
        /// Log an info message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        public static void Log(object message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Log);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        public static void Warn(object message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Warning);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        public static void Error(object message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Error);
        }
    }
}

