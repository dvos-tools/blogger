using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using com.DvosTools.blogger.Config;
using com.DvosTools.blogger.Service;
using TMPro;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers.Terminal
{
    public class TerminalHandler : ILoggingHandler
    {
        private readonly BLoggerConfig _config;
        private GameObject _inputListenerObject;
        private GameObject _terminalInstance;
        private TextMeshProUGUI _logTextComponent;
        private TMP_InputField _commandInputField;
        private UnityEngine.UI.Button _sendButton;
        private readonly StringBuilder _logBuilder = new();
        private readonly Queue<string> _logEntries = new();
        private bool _isVisible = true;

        private const string StartString = "Q:\\>";

        public TerminalHandler(BLoggerConfig config)
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

            // Only parse tokens for normal log messages, not for errors/warnings
            // This prevents error messages containing example syntax from being parsed
            string parsedLog = type == LogType.Log 
                ? ParseTerminalTokensAndActions(logString)
                : logString;

            // Format the log entry with color based on type
            string colorCode = TerminalHelper.GetColorForLogType(type);
            
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
                // Register terminal commands
                RegisterTerminalCommands();
                
                // Create a persistent input listener object that stays active
                _inputListenerObject = new GameObject("BLogger_InputListener");
                UnityEngine.Object.DontDestroyOnLoad(_inputListenerObject);
                
                // Instantiate terminal and navigate hierarchy: Canvas -> Terminal Panel -> Scroll View -> Viewport -> Log Text
                _terminalInstance = UnityEngine.Object.Instantiate(_config.onScreenTerminalPrefab);
                UnityEngine.Object.DontDestroyOnLoad(_terminalInstance);
                
                var terminalPanel = _terminalInstance.transform.Find("Terminal Panel");
                
                _logTextComponent = terminalPanel
                    .Find("Scroll View")
                    .Find("Viewport")
                    .Find("Log Text")
                    .GetComponent<TextMeshProUGUI>();
                
                // Find the command input field and send button
                _commandInputField = terminalPanel
                    .Find("Command Input")
                    .GetComponent<TMP_InputField>();
                
                _sendButton = terminalPanel
                    .Find("Send Button")
                    .GetComponent<UnityEngine.UI.Button>();
                
                // Configure text wrapping and overflow
                _logTextComponent.overflowMode = TextOverflowModes.Overflow;
                _logTextComponent.richText = true;
                _logTextComponent.parseCtrlCharacters = true;
                
                // Enable text selection for copy/paste
                _logTextComponent.enableWordWrapping = true;
                _logTextComponent.isTextObjectScaleStatic = false;
                
                _logTextComponent.text = $"<color=green>{StartString} OnScreen Terminal | Initialized</color>\n";
                _logEntries.Enqueue($"<color=green>{StartString} OnScreen Terminal | Initialized</color>");
                
                // Set up input field to handle Enter key
                _commandInputField.onSubmit.AddListener(OnCommandSubmitted);
                
                // Set up send button
                _sendButton.onClick.AddListener(OnSendButtonClicked);
                
                // Add a toggle component to the persistent input listener (not the terminal itself)
                // This ensures it keeps receiving input even when the terminal is hidden
                var toggleComponent = _inputListenerObject.AddComponent<TerminalToggleComponent>();
                toggleComponent.config = _config;
                toggleComponent.OnToggle = ToggleVisibility;
                
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerminalHandler] Failed to initialize: {ex.Message}");
                IsEnabled = false;
            }
        }
        
        private void RegisterTerminalCommands()
        {
            var registry = TerminalCommandRegistry.Instance;
            var commands = BuiltInTerminalCommands.GetCommandDefinitions(
                clearAction: _ => ClearLogs(),
                helpAction: HandleHelpCommand,
                copyAction: _ => CopyToClipboard(),
                contextAction: _ => ShowContext(),
                exitAction: _ => CloseTerminal()
            );
            
            foreach (var cmd in commands.Values) registry.RegisterCommand(new TerminalCommandRegistry.TerminalCommand(cmd.Name, cmd.Description, cmd.Usage, rawArgs => cmd.ExecuteAction(new CommandArgs(rawArgs)), cmd.Aliases));
        }
        
        private void HandleHelpCommand(CommandArgs args)
        {
            var registry = TerminalCommandRegistry.Instance;
            
            if (args.HasArgs)
            {
                var helpText = registry.GenerateCommandHelp(args.Get(0));
                BLogger.Log(helpText);
            }
            else
            {
                var helpText = registry.GenerateHelpText();
                BLogger.Log(helpText);
            }
        }
        
        private void CopyToClipboard()
        {
            if (_logTextComponent != null)
            {
                // Remove rich text tags for clean copy
                var textToCopy = Regex.Replace(_logTextComponent.text, "<.*?>", string.Empty);
                GUIUtility.systemCopyBuffer = textToCopy;
                BLogger.Log("Terminal output copied to clipboard!");
            }
        }
        
        private void ShowContext()
        {
            var context = Context.LoggingContext.GetFormattedContext();
            BLogger.Log($"Logging Context: [{context}]");
        }
        
        private void CloseTerminal()
        {
            if (!_terminalInstance) return;
            
            _isVisible = false;
            _terminalInstance.SetActive(false);
            
            Debug.Log("[TerminalHandler] Terminal closed");
        }
        
        private void ToggleVisibility()
        {
            if (!_terminalInstance) return;
            
            _isVisible = !_isVisible;
            _terminalInstance.SetActive(_isVisible);
            
            Debug.Log($"[TerminalHandler] Terminal visibility toggled: {(_isVisible ? "Shown" : "Hidden")}");
        }
        
        private void OnSendButtonClicked()
        {
            ExecuteCommand();
        }
        
        private void OnCommandSubmitted(string text)
        {
            ExecuteCommand();
        }
        
        private void ExecuteCommand()
        {
            if (_commandInputField == null) return;
            
            string command = _commandInputField.text.Trim();
            if (string.IsNullOrWhiteSpace(command)) return;
            
            // Log the command being executed
            ProcessCommand(command);
            
            _commandInputField.text = "";
            _commandInputField.ActivateInputField();
        }
        
        private void ProcessCommand(string command)
        {
            var registry = TerminalCommandRegistry.Instance;
            
            // Try to execute as a registered command first
            if (registry.TryExecuteCommand(command, out _)) return;
            
            // Check if command contains @ or ! tokens
            bool hasTokens = command.Contains("@") || command.Contains("!");
            if (hasTokens)
            {
                // Validate syntax before processing
                if (!ValidateTokenSyntax(command, out string syntaxError))
                {
                    BLogger.Error($"Syntax error: {syntaxError}");
                    return;
                }
                
                // Process tokens and display results through the logger
                // The ParseTerminalTokensAndActions method will handle @values and !actions
                BLogger.Log(command);
            }
            else
            {
                // Show simple not found message
                BLogger.Warn($"Unknow command: \"{command}\"");
            }
        }
        
        private bool ValidateTokenSyntax(string input, out string error)
        {
            error = null;
            
            // Check for unmatched parentheses in actions
            if (!input.Contains("!")) return true;
            int openParens = input.Count(c => c == '(');
            int closeParens = input.Count(c => c == ')');
                
            if (openParens != closeParens)
            {
                error = "Unmatched parentheses in action syntax. Expected format: !action(args)";
                return false;
            }
                
            // Check if action has parentheses
            // Look for ! followed by identifier that ends without a parenthesis
            // (?![(\w\.]) ensures we're at the end of the identifier and there's no opening paren
            var actionRegex = new Regex(@"![\w\.]+(?![(\w\.])");
            if (!actionRegex.IsMatch(input)) return true;
            
            error = $"Failed to Parse: {input} Actions require parentheses. Expected format: !action() or !action(args)";
            return false;

        }
        
        private void ClearLogs()
        {
            _logEntries.Clear();
            _logBuilder.Clear();
            _logTextComponent.text = $"<color=green>{StartString} Console cleared</color>\n";
            _logEntries.Enqueue($"<color=green>{StartString} Console cleared</color>");
        }

        public void Shutdown()
        {
            // Clean up event listeners
            if (_commandInputField != null)
                _commandInputField.onSubmit.RemoveListener(OnCommandSubmitted);
            
            if (_sendButton != null)
                _sendButton.onClick.RemoveListener(OnSendButtonClicked);
            
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
            _commandInputField = null;
            _sendButton = null;
            _logEntries.Clear();
            _logBuilder.Clear();
            IsEnabled = false;
            
            Debug.Log("[TerminalHandler] Shutdown complete.");
        }

        public bool IsEnabled { get; private set; }
        
      

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

                    return $"<color=red>Unknown action: \"!{actionToken}\"</color>";
                });
            }

            // Then, handle value tokens (@valueName)
            if (input.Contains("@"))
            {
                var valueRegex = new Regex(@"@([\w]+(?:\.[\w]+(?:\.[\w]+)?)?)");
                input = valueRegex.Replace(input, match =>
                {
                    var token = match.Groups[1].Value;

                    if (!TerminalValueRegistry.Instance.TryGetValue(token, out var value))
                        return $"<color=red>Unknown token: @{token}</color>";
                    
                    var valueStr = value?.ToString() ?? "null";
                    // Escape value string for display
                    valueStr = valueStr.Replace("<", "&lt;").Replace(">", "&gt;");
                        
                    // Colorize the value path
                    var colorizedToken = TerminalHelper.ColorizeValuePath(token);
                    return $"<color=red>@</color>{colorizedToken}<color=white>=</color>{valueStr}";

                });
            }

            return input;
        }

       

        private static string ColorizeActionPath(string actionToken)
        {
            // Extract path and arguments: heal(50) or Players.player1.heal(50)
            var openParen = actionToken.IndexOf('(');
            if (openParen == -1)
                return $"<color=white>{actionToken}</color>";

            var path = actionToken.Substring(0, openParen);
            var argsWithParen = actionToken.Substring(openParen); // (50)
            
            var parts = path.Split('.');

            return parts.Length switch
            {
                1 => $"<color=white>{parts[0]}</color><color=grey>{argsWithParen}</color>",
                3 => $"<color=red>{parts[0]}</color>.<color=#ADD8E6>{parts[1]}</color>.<color=white>{parts[2]}</color><color=grey>{argsWithParen}</color>",
                _ => $"<color=white>{path}</color><color=grey>{argsWithParen}</color>"
            };
        }
        
    }
}