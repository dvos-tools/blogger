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
            _handlers.ForEach(handler => 
            {
                if (handler.IsEnabled)
                {
                    handler.HandleLog(logString, stackTrace, type);
                }
            });
        }
        
        private void LoadHandlersFromConfig()
        {
            var config = BLoggerConfig.Instance;
            var newHandlers = config.CreateHandlers();
            _handlers.AddRange(newHandlers);
        }
    }
}