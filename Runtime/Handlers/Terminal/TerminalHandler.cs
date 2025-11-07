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
        private UnityEngine.UI.ScrollRect _scrollRect;
        private TerminalResizeHandle _resizeHandle;
        private readonly StringBuilder _logBuilder = new();
        private readonly Queue<string> _logEntries = new();
        private bool _isVisible = true;
        private bool _autoScroll = true; // Track if we should auto-scroll

        // Command history
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;
        private string _currentInput = "";

        private const string StartString = "Q:\\>";

        public TerminalHandler(BLoggerConfig config)
        {
            _config = config;
        }
        
        private class TerminalToggleComponent : MonoBehaviour
        {
            public BLoggerConfig config;
            public Action onToggle;
            public Action onUpArrow;
            public Action onDownArrow;
            public Action onFontSizeIncrease;
            public Action onFontSizeDecrease;
            
            private void Update()
            {
                if (InputService.IsToggleKeyPressed(config)) onToggle?.Invoke();
                if (InputService.IsUpArrowPressed(config)) onUpArrow?.Invoke();
                if (InputService.IsDownArrowPressed(config)) onDownArrow?.Invoke();
                if (InputService.IsFontSizeIncreasePressed(config)) onFontSizeIncrease?.Invoke();
                if (InputService.IsFontSizeDecreasePressed(config)) onFontSizeDecrease?.Invoke();
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
            
            // Only auto-scroll if user hasn't manually scrolled up
            if (!_scrollRect || !_autoScroll) return;
            // Use Canvas.ForceUpdateCanvases to ensure layout is updated before scrolling
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
        
        private void OnScrollChanged(Vector2 scrollPosition)
        {
            // If user scrolls up from the bottom, disable auto-scroll
            // If they scroll back to the bottom, re-enable it
            const float threshold = 0.01f; // Small threshold for floating point comparison
            _autoScroll = _scrollRect.verticalNormalizedPosition <= threshold;
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
                var scrollView = terminalPanel.Find("Scroll View");
                var viewport = scrollView.Find("Viewport");
                
                // Get the existing Log Text component
                _logTextComponent = viewport
                    .Find("Log Text")
                    .GetComponent<TextMeshProUGUI>();
                
                // Get the Content RectTransform that the ScrollRect uses
                var content = viewport.Find("Content").GetComponent<RectTransform>();
                
                // Find the command input field and send button
                _commandInputField = terminalPanel
                    .Find("Command Input")
                    .GetComponent<TMP_InputField>();
                
                _sendButton = terminalPanel
                    .Find("Send Button")
                    .GetComponent<UnityEngine.UI.Button>();
                
                // Get the ScrollRect component
                _scrollRect = scrollView.GetComponent<UnityEngine.UI.ScrollRect>();
                
                // Set up the resize handle
                var resizeButton = terminalPanel.Find("ReSize");
                if (resizeButton)
                {
                    _resizeHandle = resizeButton.gameObject.GetComponent<TerminalResizeHandle>();
                    if (!_resizeHandle) _resizeHandle = resizeButton.gameObject.AddComponent<TerminalResizeHandle>();
                    _resizeHandle.Initialize(terminalPanel.GetComponent<RectTransform>(), 200f, 1000f);
                }
                else
                {
                    Debug.LogWarning("[TerminalHandler] ReSize button not found in Terminal Panel");
                }
                
                // CRITICAL: Move Log Text to be a child of Content so ScrollRect works properly
                _logTextComponent.transform.SetParent(content, false);
                
                // Reset Log Text anchors and position to fill the Content area
                var logTextRect = _logTextComponent.GetComponent<RectTransform>();
                logTextRect.anchorMin = new Vector2(0, 1); // Top-left anchor
                logTextRect.anchorMax = new Vector2(1, 1); // Top-right anchor
                logTextRect.pivot = new Vector2(0.5f, 1); // Pivot at top
                logTextRect.anchoredPosition = Vector2.zero;
                logTextRect.offsetMin = new Vector2(10, logTextRect.offsetMin.y); // Left padding
                logTextRect.offsetMax = new Vector2(-10, logTextRect.offsetMax.y); // Right padding
                
                // Ensure the ScrollRect can receive input
                var scrollViewImage = scrollView.GetComponent<UnityEngine.UI.Image>();
                if (scrollViewImage != null)
                {
                    scrollViewImage.raycastTarget = true;
                    // Set a minimal alpha to ensure raycasts work
                    var color = scrollViewImage.color;
                    color.a = 0.01f;
                    scrollViewImage.color = color;
                }
                
                // Ensure viewport Image also receives raycasts
                var viewportImage = viewport.GetComponent<UnityEngine.UI.Image>();
                if (viewportImage != null)
                {
                    viewportImage.raycastTarget = true;
                    var color = viewportImage.color;
                    if (color.a == 0) color.a = 0.01f;
                    viewportImage.color = color;
                }
                
                // Listen to scroll events to detect manual scrolling
                _scrollRect.onValueChanged.AddListener(OnScrollChanged);
                
                // Disable horizontal scrolling, we only want vertical
                _scrollRect.horizontal = false;
                _scrollRect.vertical = true;
                
                // Configure text wrapping and overflow
                _logTextComponent.overflowMode = TextOverflowModes.Overflow;
                _logTextComponent.richText = true;
                _logTextComponent.parseCtrlCharacters = true;
                
                // Enable text selection for copy/paste
                _logTextComponent.enableWordWrapping = true;
                _logTextComponent.isTextObjectScaleStatic = false;
                
                // Add ContentSizeFitter to the Content object to resize based on text
                var contentSizeFitter = content.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                }
                contentSizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                contentSizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add VerticalLayoutGroup to Content to properly size based on children
                var layoutGroup = content.gameObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                }
                layoutGroup.childControlHeight = true;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                
                // Add LayoutElement to Log Text so VerticalLayoutGroup sizes it correctly
                var layoutElement = _logTextComponent.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = _logTextComponent.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                }
                layoutElement.preferredHeight = -1;
                layoutElement.flexibleHeight = 1;
                
                _logTextComponent.text = $"<color=green>{StartString} OnScreen Terminal | Initialized</color>\n";
                _logEntries.Enqueue($"<color=green>{StartString} OnScreen Terminal | Initialized</color>");
                
                // Set up input field to handle Enter key
                _commandInputField.onSubmit.AddListener(_ => ExecuteCommand());
                
                // Set up send button
                _sendButton.onClick.AddListener(ExecuteCommand);
                
                // Set initial font size from config
                if (_logTextComponent) _logTextComponent.fontSize = _config.terminalFontSize;
                
                // Add a toggle component to the persistent input listener (not the terminal itself)
                // This ensures it keeps receiving input even when the terminal is hidden
                var toggleComponent = _inputListenerObject.AddComponent<TerminalToggleComponent>();
                toggleComponent.config = _config;
                toggleComponent.onToggle = ToggleVisibility;
                toggleComponent.onUpArrow = NavigateHistoryUp;
                toggleComponent.onDownArrow = NavigateHistoryDown;
                toggleComponent.onFontSizeIncrease = IncreaseFontSize;
                toggleComponent.onFontSizeDecrease = DecreaseFontSize;
                
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
            if (_logTextComponent == null) return;
            // Remove rich text tags for clean copy
            var textToCopy = Regex.Replace(_logTextComponent.text, "<.*?>", string.Empty);
            GUIUtility.systemCopyBuffer = textToCopy;
            BLogger.Log("Terminal output copied to clipboard!");
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
        }
        
        private void ExecuteCommand()
        {
            if (_commandInputField == null) return;
            
            string command = _commandInputField.text.Trim();
            if (string.IsNullOrWhiteSpace(command)) return;
            
            // Add to command history
            _commandHistory.Add(command);
            _historyIndex = _commandHistory.Count; // Reset to end of history
            _currentInput = "";
            
            // Re-enable auto-scroll so command output is visible at the bottom
            _autoScroll = true;
            
            // Log the command being executed
            ProcessCommand(command);
            
            _commandInputField.text = "";
            _commandInputField.ActivateInputField();
        }

        private void NavigateHistoryUp()
        {
            if (!_isVisible || !_commandInputField || _commandHistory.Count == 0) return;
            
            // Save current input if we're at the end of history
            if (_historyIndex == _commandHistory.Count)
                _currentInput = _commandInputField.text;
            
            // Move back in history
            if (_historyIndex <= 0) return;
            _historyIndex--;
            _commandInputField.text = _commandHistory[_historyIndex];
            _commandInputField.MoveToEndOfLine(false, false); // Move the cursor to end
        }

        private void NavigateHistoryDown()
        {
            if (!_isVisible || _commandInputField == null || _commandHistory.Count == 0) return;
            
            // Move forward in history
            if (_historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                _commandInputField.text = _commandHistory[_historyIndex];
                _commandInputField.MoveToEndOfLine(false, false); // Move the cursor to end
            }
            else if (_historyIndex == _commandHistory.Count - 1)
            {
                // Reached the end, restore current input
                _historyIndex = _commandHistory.Count;
                _commandInputField.text = _currentInput;
                _commandInputField.MoveToEndOfLine(false, false); // Move the cursor to end
            }
        }

        private void IncreaseFontSize()
        {
            if (!_logTextComponent) return;
            
            float currentSize = _logTextComponent.fontSize;
            float newSize = Mathf.Min(currentSize + 2, _config.maxTerminalFontSize);
            
            if (Mathf.Abs(newSize - currentSize) > 0.01f) _logTextComponent.fontSize = newSize;
        }

        private void DecreaseFontSize()
        {
            if (!_logTextComponent) return;
            
            float currentSize = _logTextComponent.fontSize;
            float newSize = Mathf.Max(currentSize - 2, _config.minTerminalFontSize);
            
            if (Mathf.Abs(newSize - currentSize) > 0.01f) _logTextComponent.fontSize = newSize;
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
            if (_commandInputField)
                _commandInputField.onSubmit.RemoveAllListeners();
            
            if (_sendButton)
                _sendButton.onClick.RemoveAllListeners();
            
            if (_scrollRect)
                _scrollRect.onValueChanged.RemoveAllListeners();
            
            // Clean up resize handle
            _resizeHandle = null;
            
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
            _scrollRect = null;
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

                    if (!TerminalValueRegistry.Instance.TryExecuteAction(actionToken, out var result))
                        return $"<color=red>Unknown action: \"!{actionToken}\"</color>";
                    
                    // Colorize the action path
                    var colorizedAction = ColorizeActionPath(actionToken);

                    if (result == null) return $"<color=red>Executed action: </color> {colorizedAction} [Action executed]";
                        
                    var resultStr = result.ToString();
                    // Escape result string for display
                    resultStr = resultStr.Replace("<", "&lt;").Replace(">", "&gt;");
                    return $"<color=red>Executed action: </color> {colorizedAction} [Returned: {resultStr}]";

                });
            }

            // Then, handle value tokens (@valueName)
            if (!input.Contains("@")) return input;
            
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

            return input;
        }

       

        private static string ColorizeActionPath(string actionToken)
        {
            // Extract a path and arguments: heal(50) or Players.player1.heal(50)
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