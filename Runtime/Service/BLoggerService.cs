using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.DvosTools.blogger.Handlers;
using com.DvosTools.blogger.Config;
using com.DvosTools.blogger.Context;
using com.DvosTools.blogger.Handlers.Terminal;

namespace com.DvosTools.blogger.Service
{
    public class BLoggerService
    {
        private static BLoggerService _instance;
        public static BLoggerService Instance => _instance ??= new BLoggerService();
        private readonly List<ILoggingHandler> _handlers = new();
        
        // Thread-local flag to mark when BLogger itself is making a Debug.Log call
        // This prevents the UnityLogCatcher from re-processing BLogger's own Debug logs
        [ThreadStatic]
        internal static bool IsBLoggerDebugLog;
        
        private BLoggerService()
        {
            LoadHandlersFromConfig();
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Inject logging context into the log message BEFORE sending to handlers
            // This ensures ALL handlers (Console, File, Loki, etc.) get enriched logs
            var context = LoggingContext.GetFormattedContext();
            var enrichedLogString = $"[{context}] {logString}";
            
            List<Exception> exceptions = new List<Exception>();

            foreach (var handler in _handlers.Where(handler => handler.IsEnabled))
            {
                try
                {
                    // TerminalHandler gets raw log without context prefix (context available via command)
                    var logToSend = handler is TerminalHandler ? logString : enrichedLogString;
                    handler.HandleLog(logToSend, stackTrace, type);
                }
                catch (Exception ex)
                {
                    // Collect all exceptions but continue processing other handlers
                    exceptions.Add(ex);
                }
            }

            // If any handlers threw exceptions, re-throw them all as an AggregateException
            if (exceptions.Count > 0)
            {
                throw new AggregateException("[BLogger] One or more handlers failed to process the log message", exceptions);
            }
        }
        
        private void LoadHandlersFromConfig()
        {
            var config = BLoggerConfig.Instance;
            var newHandlers = config.CreateHandlers();
            
            // Initialize all handlers
            foreach (var handler in newHandlers)
            {
                handler.Initialize();
            }
            
            _handlers.AddRange(newHandlers);
        }
    }
}