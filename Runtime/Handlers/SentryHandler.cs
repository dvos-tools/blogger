using System;
using System.Net.Http;
using System.Text;
using com.DvosTools.blogger.Config;
using Sentry.Unity;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers
{
    public class SentryHandler: ILoggingHandler
    {
        private readonly BLoggerConfig _config;
        private static readonly HttpClient Client = new();
        private string _lokiURL;
        public bool IsEnabled { get; set; }

        public SentryHandler(BLoggerConfig config)
        {
            _config = config;
            _lokiURL = _config.lokiUrl+"/loki/api/v1/push";
        }

        public void Initialize()
        {
            if (_config.enableSentry)
            {
                SentryUnity.Init(o =>
                {
                    o.Dsn = _config.sentryUrl;
                    o.TracesSampleRate = 1.0;
                    o.Debug = true;
                });
            }
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
                var json = $@"{{
                    ""streams"": [
                        {{
                            ""stream"": {{ ""app"": ""unity-client"", ""level"": ""{type}"" }},
                            ""values"": [[ ""{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000}"", ""{logString}\n{stackTrace}"" ]]
                        }}
                    ]
                }}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await Client.PostAsync(_lokiURL, content);
            }
            catch (Exception e)
            {
                BLogger.Error(e);
            }
        }
    }
}