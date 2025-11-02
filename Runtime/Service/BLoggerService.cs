using System;
using System.Collections.Generic;
using UnityEngine;
using com.DvosTools.blogger.Handlers;
using com.DvosTools.blogger.Config;
using com.DvosTools.blogger.Context;

namespace com.DvosTools.blogger.Service
{
    public class BLoggerService
    {
        private static BLoggerService _instance;
        public static BLoggerService Instance => _instance ??= new BLoggerService();
        private readonly List<ILoggingHandler> _handlers = new();
        
        // Rate limiting
        private float _lastResetTime;
        private int _logsThisSecond;
        
        private BLoggerService()
        {
            LoadHandlersFromConfig();
            _lastResetTime = Time.realtimeSinceStartup;
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            var config = BLoggerConfig.Instance;
            
            // Apply sampling if enabled
            if (config.enableSampling && !ShouldLogMessage(type, config))
            {
                return; // Skip this log due to sampling
            }
            
            // Apply rate limiting if configured
            if (config.maxLogsPerSecond > 0 && !CheckRateLimit(config))
            {
                return; // Skip this log due to rate limiting
            }
            
            // Inject logging context into the log message BEFORE sending to handlers
            // This ensures ALL handlers (Console, File, Loki, etc.) get enriched logs
            var context = LoggingContext.GetFormattedContext();
            var enrichedLogString = $"[{context}] {logString}";
            
            List<Exception> exceptions = new List<Exception>();
            
            foreach (var handler in _handlers)
            {
                if (!handler.IsEnabled) continue;
                
                try
                {
                    handler.HandleLog(enrichedLogString, stackTrace, type);
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
        
        /// <summary>
        /// Determines if a log message should be recorded based on sampling configuration
        /// </summary>
        private bool ShouldLogMessage(LogType type, BLoggerConfig config)
        {
            // Always log exceptions if configured
            if (type == LogType.Exception && config.alwaysLogExceptions)
            {
                return true;
            }
            
            // Get sample rate based on log type
            float sampleRate = type switch
            {
                LogType.Log => config.infoSampleRate,
                LogType.Warning => config.warningSampleRate,
                LogType.Error => config.errorSampleRate,
                LogType.Exception => config.errorSampleRate,
                LogType.Assert => config.errorSampleRate,
                _ => 1.0f
            };
            
            // Sample based on random value
            return UnityEngine.Random.value <= sampleRate;
        }
        
        /// <summary>
        /// Check if we're within the rate limit (logs per second)
        /// </summary>
        private bool CheckRateLimit(BLoggerConfig config)
        {
            float currentTime = Time.realtimeSinceStartup;
            
            // Reset counter every second
            if (currentTime - _lastResetTime >= 1.0f)
            {
                _lastResetTime = currentTime;
                _logsThisSecond = 0;
            }
            
            // Check if we're under the limit
            if (_logsThisSecond >= config.maxLogsPerSecond)
            {
                return false; // Rate limit exceeded
            }
            
            _logsThisSecond++;
            return true;
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