using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using com.DvosTools.blogger.Config;
using com.DvosTools.blogger.Service;

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

            // Sanitize and escape special characters for display
            string sanitizedLog = SanitizeLogString(logString);

            // Parse and replace @tokens with actual values, and execute !actions
            string parsedLog = ParseTerminalTokensAndActions(sanitizedLog);

            // Format the log entry with color based on type
            string colorCode = GetColorForLogType(type);
            
            // Split long lines to ensure wrapping works inside color tags
            parsedLog = EnsureWrappableContent(parsedLog);
            
            // Create formatted log entry with terminal-style prompt
            string formattedLog = $"<color={colorCode}>{StartString} {parsedLog}</color>";
            
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
                
                // Configure text wrapping and overflow
                _logTextComponent.overflowMode = TextOverflowModes.Overflow;
                _logTextComponent.richText = true;
                _logTextComponent.parseCtrlCharacters = true;
                
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
                LogType.Exception => "red",
                _ => "white"
            };
        }

        private string SanitizeLogString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Replace control characters with visible representations
            input = input.Replace("\r\n", "\n")  // Normalize line endings
                         .Replace("\r", "\n")    // Normalize line endings
                         .Replace("\t", "    "); // Replace tabs with spaces

            // Escape characters that might interfere with TextMeshPro rich text
            // But preserve our @ and ! tokens for processing
            input = input.Replace("<", "&lt;")  // Escape < unless it's part of our tags
                         .Replace(">", "&gt;"); // Escape >

            // Insert zero-width spaces after certain characters to allow wrapping in long strings
            input = InsertWrappingHints(input);

            return input;
        }

        private string InsertWrappingHints(string input)
        {
            // Insert zero-width space (U+200B) after these characters to help TextMeshPro wrap long strings
            // This is especially helpful for long paths, URLs, or continuous text
            var result = new StringBuilder(input.Length * 2);
            
            for (int i = 0; i < input.Length; i++)
            {
                result.Append(input[i]);
                
                // Add zero-width space after certain characters to enable wrapping
                if (i < input.Length - 1) // Don't add at the end
                {
                    char current = input[i];
                    char next = input[i + 1];
                    
                    // Add wrapping hints after punctuation, slashes, colons, etc.
                    if (current == '/' || current == '\\' || current == '.' || 
                        current == ':' || current == '-' || current == '_' ||
                        current == ',' || current == ';' || current == '|')
                    {
                        // Don't add if next is space (already wrappable)
                        if (next != ' ')
                        {
                            result.Append('\u200B'); // Zero-width space
                        }
                    }
                }
            }
            
            return result.ToString();
        }

        private string ParseTerminalTokensAndActions(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // First, handle actions (!actionName(args))
            if (input.Contains("!"))
            {
                var actionRegex = new Regex(@"!([\w\.]+\([^\)]*\))");
                input = actionRegex.Replace(input, match =>
                {
                    var actionToken = match.Groups[1].Value;
                    
                    if (TerminalValueRegistry.Instance.TryExecuteAction(actionToken, out var result))
                    {
                        // Colorize the action path
                        var colorizedAction = ColorizeActionPath(actionToken);
                        
                        if (result != null)
                        {
                            var resultStr = result.ToString();
                            // Escape result string for display
                            resultStr = resultStr.Replace("<", "&lt;").Replace(">", "&gt;");
                            return $"<color=red>!</color>{colorizedAction} <color=cyan>[Returned: {resultStr}]</color>";
                        }
                        return $"<color=red>!</color>{colorizedAction} <color=cyan>[Action executed]</color>";
                    }

                    return $"<color=red>!{actionToken}?</color>";
                });
            }

            // Then, handle value tokens (@valueName)
            if (input.Contains("@"))
            {
                var valueRegex = new Regex(@"@([\w]+(?:\.[\w]+(?:\.[\w]+)?)?)");
                input = valueRegex.Replace(input, match =>
                {
                    var token = match.Groups[1].Value;
                    
                    if (TerminalValueRegistry.Instance.TryGetValue(token, out var value))
                    {
                        var valueStr = value?.ToString() ?? "null";
                        // Escape value string for display
                        valueStr = valueStr.Replace("<", "&lt;").Replace(">", "&gt;");
                        
                        // Colorize the value path
                        var colorizedToken = ColorizeValuePath(token);
                        return $"<color=red>@</color>{colorizedToken}<color=white>=</color>{valueStr}";
                    }

                    return $"<color=red>@{token}?</color>";
                });
            }

            return input;
        }

        private string ColorizeValuePath(string token)
        {
            var parts = token.Split('.');
            
            if (parts.Length == 1)
            {
                // Static value: fps
                return $"<color=white>{parts[0]}</color>";
            }
            else if (parts.Length == 3)
            {
                // Instance value: Players.player1.health
                // Aggregate=red, InstanceKey=lightblue, ValueName=white
                return $"<color=red>{parts[0]}</color>.<color=#ADD8E6>{parts[1]}</color>.<color=white>{parts[2]}</color>";
            }
            
            return $"<color=white>{token}</color>";
        }

        private string ColorizeActionPath(string actionToken)
        {
            // Extract path and arguments: heal(50) or Players.player1.heal(50)
            var openParen = actionToken.IndexOf('(');
            if (openParen == -1)
                return $"<color=white>{actionToken}</color>";

            var path = actionToken.Substring(0, openParen);
            var argsWithParen = actionToken.Substring(openParen); // (50)
            
            var parts = path.Split('.');
            
            if (parts.Length == 1)
            {
                // Static action: pause
                return $"<color=white>{parts[0]}</color><color=grey>{argsWithParen}</color>";
            }
            else if (parts.Length == 3)
            {
                // Instance action: Players.player1.heal
                // Aggregate=red, InstanceKey=lightblue, ActionName=white
                return $"<color=red>{parts[0]}</color>.<color=#ADD8E6>{parts[1]}</color>.<color=white>{parts[2]}</color><color=grey>{argsWithParen}</color>";
            }
            
            return $"<color=white>{path}</color><color=grey>{argsWithParen}</color>";
        }

        private string EnsureWrappableContent(string input)
        {
            // If line is extremely long (over 200 chars), break it up to help TextMeshPro
            const int maxLineLength = 200;
            
            if (input.Length <= maxLineLength)
                return input;

            var result = new StringBuilder(input.Length + 100);
            var lines = input.Split('\n');

            foreach (var line in lines)
            {
                if (line.Length <= maxLineLength)
                {
                    result.AppendLine(line);
                }
                else
                {
                    // Break long lines into chunks
                    for (int i = 0; i < line.Length; i += maxLineLength)
                    {
                        int length = Mathf.Min(maxLineLength, line.Length - i);
                        result.AppendLine(line.Substring(i, length));
                    }
                }
            }

            return result.ToString().TrimEnd('\n', '\r');
        }
    }
}