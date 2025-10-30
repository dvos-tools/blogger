using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using com.DvosTools.blogger.Config;

namespace com.DvosTools.blogger.Handlers
{
    public class OnScreenHandler : ILoggingHandler
    {
        private readonly BLoggerConfig _config;
        private GameObject _inputListenerObject;
        private GameObject _terminalInstance;
        private TextMeshProUGUI _logTextComponent;
        private readonly StringBuilder _logBuilder = new();
        private readonly Queue<string> _logEntries = new();
        private bool _isVisible = true;

        private const string StartString = "Q:\\>";

        public OnScreenHandler(BLoggerConfig config)
        {
            _config = config;
        }
        
        private class TerminalToggleComponent : MonoBehaviour
        {
            public BLoggerConfig config;
            public Action OnToggle;
            
            private void Update()
            {
                if (Service.InputService.IsToggleKeyPressed(config))
                {
                    OnToggle?.Invoke();
                }
            }
        }
        
        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!IsEnabled || !_logTextComponent) return;

            // Format the log entry with color based on type
            string colorCode = GetColorForLogType(type);
            
            // Create formatted log entry with terminal-style prompt
            string formattedLog = $"<color={colorCode}>{StartString} {logString}</color>";
            
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
                // Create a persistent input listener object that stays active
                _inputListenerObject = new GameObject("BLogger_InputListener");
                UnityEngine.Object.DontDestroyOnLoad(_inputListenerObject);
                
                // Instantiate terminal and navigate hierarchy: Canvas -> Terminal Panel -> Scroll View -> Viewport -> Log Text
                _terminalInstance = UnityEngine.Object.Instantiate(_config.onScreenTerminalPrefab);
                UnityEngine.Object.DontDestroyOnLoad(_terminalInstance);
                
                _logTextComponent = _terminalInstance.transform
                    .Find("Terminal Panel")
                    .Find("Scroll View")
                    .Find("Viewport")
                    .Find("Log Text")
                    .GetComponent<TextMeshProUGUI>();
                
                _logTextComponent.text = $"<color=green>{StartString} OnScreen Terminal | Initialized</color>\n";
                _logEntries.Enqueue($"<color=green>{StartString} OnScreen Terminal | Initialized</color>");
                
                // Add a toggle component to the persistent input listener (not the terminal itself)
                // This ensures it keeps receiving input even when the terminal is hidden
                var toggleComponent = _inputListenerObject.AddComponent<TerminalToggleComponent>();
                toggleComponent.config = _config;
                toggleComponent.OnToggle = ToggleVisibility;
                
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OnScreenHandler] Failed to initialize: {ex.Message}");
                IsEnabled = false;
            }
        }
        
        private void ToggleVisibility()
        {
            if (!_terminalInstance) return;
            
            _isVisible = !_isVisible;
            _terminalInstance.SetActive(_isVisible);
            
            Debug.Log($"[OnScreenHandler] Terminal visibility toggled: {(_isVisible ? "Shown" : "Hidden")}");
        }

        public void Shutdown()
        {
            if (_terminalInstance)
            {
                UnityEngine.Object.Destroy(_terminalInstance);
                _terminalInstance = null;
            }
            
            if (_inputListenerObject)
            {
                UnityEngine.Object.Destroy(_inputListenerObject);
                _inputListenerObject = null;
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
            // TODO: Find some good colours for this
            return type switch
            {
                LogType.Error => "red",
                LogType.Assert => "blue",
                LogType.Warning => "yellow",
                LogType.Log => "white",
                LogType.Exception => "yellow",
                _ => "white"
            };
        }
    }
}