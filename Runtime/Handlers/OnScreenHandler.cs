using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using com.DvosTools.blogger.Config;

namespace com.DvosTools.blogger.Handlers
{
    /// <summary>
    /// On-screen terminal handler that displays logs in a transparent overlay similar to Quantum Console
    /// </summary>
    public class OnScreenHandler : ILoggingHandler
    {
        private readonly BLoggerConfig _config;
        private GameObject _onScreenTerminalPrefab;
        private GameObject _terminalInstance;
        private TextMeshProUGUI _logTextComponent;
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private readonly Queue<string> _logEntries = new Queue<string>();

        public OnScreenHandler(BLoggerConfig config, GameObject onScreenTerminalPrefab)
        {
            _config = config;
            _onScreenTerminalPrefab = onScreenTerminalPrefab;
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!IsEnabled || !_logTextComponent) return;

            // Format the log entry with color based on type
            string colorCode = GetColorForLogType(type);
            string logPrefix = GetPrefixForLogType(type);
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            // Create formatted log entry with terminal-style prompt
            string formattedLog = $"<color={colorCode}>~> {timestamp} | {logPrefix} | {logString}</color>";
            
            // Add to queue and manage max entries
            _logEntries.Enqueue(formattedLog);
            
            // Remove old entries if we exceed max
            while (_logEntries.Count > _config.maxOnScreenLogEntries)
            {
                _logEntries.Dequeue();
            }
            
            // Rebuild the entire log text
            _logBuilder.Clear();
            foreach (var entry in _logEntries)
            {
                _logBuilder.AppendLine(entry);
            }
            
            // Update the text component
            _logTextComponent.text = _logBuilder.ToString();
        }

        public void Initialize()
        {
            try
            {
                // Instantiate terminal and navigate hierarchy: Canvas -> Terminal Panel -> Scroll View -> Viewport -> Log Text
                _terminalInstance = UnityEngine.Object.Instantiate(_onScreenTerminalPrefab);
                UnityEngine.Object.DontDestroyOnLoad(_terminalInstance);
                
                _logTextComponent = _terminalInstance.transform
                    .Find("Terminal Panel")
                    .Find("Scroll View")
                    .Find("Viewport")
                    .Find("Log Text")
                    .GetComponent<TextMeshProUGUI>();
                
                _logTextComponent.text = "<color=green>~> OnScreen Terminal | Initialized</color>\n";
                _logEntries.Enqueue("<color=green>~> OnScreen Terminal | Initialized</color>");
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnScreenHandler] Failed to initialize: {ex.Message}");
                IsEnabled = false;
            }
        }

        public void Shutdown()
        {
            if (!_terminalInstance)
            {
                UnityEngine.Object.Destroy(_terminalInstance);
                _terminalInstance = null;
            }
            
            _logTextComponent = null;
            _logEntries.Clear();
            _logBuilder.Clear();
            IsEnabled = false;
            
            Debug.Log("[OnScreenHandler] Shutdown complete.");
        }

        public bool IsEnabled { get; private set; }
        
        private string GetColorForLogType(LogType type)
        {
            return type switch
            {
                LogType.Error => "red",
                LogType.Assert => "red",
                LogType.Warning => "yellow",
                LogType.Log => "white",
                LogType.Exception => "red",
                _ => "white"
            };
        }
        
        private string GetPrefixForLogType(LogType type)
        {
            return type switch
            {
                LogType.Error => "ERROR",
                LogType.Assert => "ASSERT",
                LogType.Warning => "WARN",
                LogType.Log => "INFO",
                LogType.Exception => "EXCEPTION",
                _ => "LOG"
            };
        }
    }
}