using System;
using System.IO;
using UnityEngine;
using com.DvosTools.blogger.Config;

namespace com.DvosTools.blogger.Handlers
{
    internal class FileHandler : ILoggingHandler
    {
        private readonly BLoggerConfig _config;
        private string _logFilePath;
        
        public bool IsEnabled { get; private set; }
        
        public FileHandler(BLoggerConfig config)
        {
            _config = config;
            Initialize();
        }
        
        public void Initialize()
        {
            try
            {
                _logFilePath = Path.Combine(Application.persistentDataPath, _config.logFilePath);
                
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(directory))
                {
                    if (directory != null) Directory.CreateDirectory(directory);
                }
                
                var startupMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] Application started - BLogger FileHandler initialized\n";
                File.AppendAllText(_logFilePath, startupMessage);
                
                // Log the full path to Unity Console for easy access (clickable link)
                Debug.Log($"[BLogger] Log file created at:");
                Debug.Log($"<a href=\"{_logFilePath}\">{_logFilePath}</a>");
                
                IsEnabled = true;
            }
            catch (Exception)
            {
                IsEnabled = false;
            }
        }
        
        public void Shutdown()
        {
            if (IsEnabled && !string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    var shutdownMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] Application shutting down - BLogger FileHandler stopped\n";
                    File.AppendAllText(_logFilePath, shutdownMessage);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            
            IsEnabled = false;
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!IsEnabled || string.IsNullOrEmpty(_logFilePath)) return;
            
            try
            {
                var formattedMessage = FormatLogMessage(logString, stackTrace, type);
                File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        private string FormatLogMessage(string logString, string stackTrace, LogType type)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"[{timestamp}] [{type}] {logString}\n{stackTrace}";
        }
        
    }
}