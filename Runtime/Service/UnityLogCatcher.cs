using System;
using UnityEngine;

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Singleton class that automatically catches all Unity logs, warnings, and errors by listening to Application.logMessageReceivedThreaded
    /// </summary>
    internal sealed class UnityLogCatcher
    {
        private static UnityLogCatcher _instance;
        private bool IsInitialized { get; set; }
        
        // Thread-local flag to prevent infinite loops from Debug.Log calls within the processing
        [ThreadStatic]
        private static bool _isProcessing;
        
        private UnityLogCatcher()
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            _instance ??= new UnityLogCatcher();
            if (_instance.IsInitialized) return;
            
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            _instance.IsInitialized = true;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoShutdown()
        {
            // Register for application quit event
            Application.quitting += () =>
            {
                if (_instance is not { IsInitialized: true }) return;
                Application.logMessageReceivedThreaded -= OnLogMessageReceived;
                _instance.IsInitialized = false;
            } ;
        }
        
        /// <summary>
        /// Handle Unity log messages received on any thread
        /// </summary>
        /// <param name="logString">The log message</param>
        /// <param name="stackTrace">The stack trace</param>
        /// <param name="type">The log type (Log, Warning, Error, Exception, Assert)</param>
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            // Prevent re-entrant calls - if we're already processing a log on this thread, ignore new ones
            // This prevents infinite loops from Debug.Log calls within the processing code
            if (_isProcessing) return;
            
            // Skip logs that come from BLogger's own Debug.Log calls
            // This prevents duplicate logging when BLogger.Log() also calls Debug.Log()
            if (BLoggerService.IsBLoggerDebugLog) return;
            
            _isProcessing = true;
            try
            {
                // Direct method calls - you can add your own processing here
                ProcessLogMessage(logString, stackTrace, type);
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        /// <summary>
        /// Process the received log message - override this method to add your own logic
        /// </summary>
        /// <param name="logString">The log message</param>
        /// <param name="stackTrace">The stack trace</param>
        /// <param name="type">The log type</param>
        private static void ProcessLogMessage(string logString, string stackTrace, LogType type)
        {
            BLoggerService.Instance.HandleLog(logString, stackTrace, type);
        }
    }
}