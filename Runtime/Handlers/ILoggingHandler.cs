using UnityEngine;

namespace com.DvosTools.blogger.Handlers
{
    /// <summary>
    /// Interface for logging handlers
    /// </summary>
    public interface ILoggingHandler
    {
        /// <summary>
        /// Handle a log message
        /// </summary>
        /// <param name="logString">The log message</param>
        /// <param name="stackTrace">The stack trace</param>
        /// <param name="type">The Unity log type</param>
        void HandleLog(string logString, string stackTrace, LogType type);
        
        /// <summary>
        /// Initialize the handler
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Shutdown the handler
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Check if the handler is enabled
        /// </summary>
        bool IsEnabled { get; }
    }
}