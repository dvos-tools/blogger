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
        public bool IsEnabled { get; private set; }

        public LokiHandler(BLoggerConfig config)
        {
            _lokiURL = config.lokiUrl + "/loki/api/v1/push";
        }

        public void Initialize()
        {
            IsEnabled = true;
        }

        public void Shutdown()
        {
            IsEnabled = false;
        }

        public async void HandleLog(string logString, string stackTrace, LogType type)
        {
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

