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

        /// <summary>
        /// Register an instance to be accessible via the terminal
        /// Use this for objects marked with [BLoggerAggregate] attribute
        /// </summary>
        /// <param name="instance">The instance to register (can be MonoBehaviour or regular object)</param>
        public static void RegisterValue(object instance)
        {
            TerminalValueRegistry.Instance.RegisterInstance(instance);
        }

        /// <summary>
        /// Unregister an instance from the terminal
        /// Call this when the instance is destroyed or no longer needed
        /// </summary>
        /// <param name="instance">The instance to unregister</param>
        public static void UnregisterValue(object instance)
        {
            TerminalValueRegistry.Instance.UnregisterInstance(instance);
        }
    }
}

