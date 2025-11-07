using System;
using System.Net.Http;
using System.Text;
using com.DvosTools.blogger.Config;
using com.DvosTools.blogger.Context;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers
{
    public class LokiHandler : ILoggingHandler
    {
        private static readonly HttpClient Client = new();
        private readonly string _lokiURL;
        private readonly BLoggerConfig _config;
        public bool IsEnabled { get; private set; }
        
        // Rate limiting
        private float _lastResetTime;
        private int _logsThisSecond;

        public LokiHandler(BLoggerConfig config)
        {
            _lokiURL = config.lokiUrl + "/loki/api/v1/push";
            _config = config;
        }

        public void Initialize()
        {
            IsEnabled = true;
            _lastResetTime = Time.realtimeSinceStartup;
        }

        public void Shutdown()
        {
            IsEnabled = false;
        }

        public async void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!ShouldLogMessage(type)) return; // Skip this log due to sampling
            if (_config.maxLogsPerSecond > 0 && !CheckRateLimit()) return; // Skip this log due to rate limiting
            
            try
            {
                // Context is already injected by BLoggerService, escape for JSON
                var escapedLog = EscapeJson(logString);
                var escapedStack = EscapeJson(stackTrace);
                
                // Build a complete log message with stack trace
                var fullMessage = escapedLog;
                if (!string.IsNullOrEmpty(escapedStack))
                {
                    fullMessage += $"\\n{escapedStack}";
                }
                
                // Send it to Loki with low-cardinality labels for efficient filtering
                var json = $@"{{
                    ""streams"": [
                        {{
                            ""stream"": {{ 
                                ""app"": ""unity-client"", 
                                ""level"": ""{type}"",
                                ""scene"": ""{LoggingContext.CurrentScene}"",
                                ""platform"": ""{LoggingContext.Platform}"",
                                ""env"": ""{LoggingContext.Environment}"",
                                ""version"": ""{LoggingContext.AppVersion}""
                            }},
                            ""values"": [[ ""{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000}"", ""{fullMessage}"" ]]
                        }}
                    ]
                }}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await Client.PostAsync(_lokiURL, content);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LokiHandler] Failed to send log to Loki: {e.Message}");
            }
        }

        /// <summary>
        /// Determines if a log message should be sent to Loki based on sampling configuration
        /// </summary>
        private bool ShouldLogMessage(LogType type)
        {
            // If sampling is disabled, always log
            if (!_config.enableSampling)
            {
                return true;
            }
            
            // Always log exceptions if configured
            if (type == LogType.Exception && _config.alwaysLogExceptions)
            {
                return true;
            }
            
            // Get sample rate based on log type
            float sampleRate = type switch
            {
                LogType.Log => _config.infoSampleRate,
                LogType.Warning => _config.warningSampleRate,
                LogType.Error => _config.errorSampleRate,
                LogType.Exception => _config.errorSampleRate,
                LogType.Assert => _config.errorSampleRate,
                _ => 1.0f
            };
            
            // Sample based on random value
            return UnityEngine.Random.value <= sampleRate;
        }
        
        /// <summary>
        /// Check if we're within the rate limit (logs per second) for Loki
        /// </summary>
        private bool CheckRateLimit()
        {
            float currentTime = Time.realtimeSinceStartup;
            
            // Reset counter every second
            if (currentTime - _lastResetTime >= 1.0f)
            {
                _lastResetTime = currentTime;
                _logsThisSecond = 0;
            }
            
            // Check if we're under the limit
            if (_logsThisSecond >= _config.maxLogsPerSecond)
            {
                return false; // Rate limit exceeded
            }
            
            _logsThisSecond++;
            return true;
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}

