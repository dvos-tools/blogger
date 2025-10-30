using System;
using System.Collections.Generic;
using UnityEngine;
using com.DvosTools.blogger.Handlers;
using com.DvosTools.blogger.Config;

namespace com.DvosTools.blogger.Service
{
    public class BLoggerService
    {
        private static BLoggerService _instance;
        public static BLoggerService Instance => _instance ??= new BLoggerService();
        private readonly List<ILoggingHandler> _handlers = new();
        
        private BLoggerService()
        {
            LoadHandlersFromConfig();
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            List<Exception> exceptions = new List<Exception>();
            
            foreach (var handler in _handlers)
            {
                if (!handler.IsEnabled) continue;
                
                try
                {
                    handler.HandleLog(logString, stackTrace, type);
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