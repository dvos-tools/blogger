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
        private readonly Queue<int> _logEntryLengths = new(); // Track lengths for efficient removal
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
            public Action onTab;

            private void Update()
            {
                if (InputService.IsToggleKeyPressed(config)) onToggle?.Invoke();
                if (InputService.IsUpArrowPressed(config)) onUpArrow?.Invoke();
                if (InputService.IsDownArrowPressed(config)) onDownArrow?.Invoke();
                if (InputService.IsFontSizeIncreasePressed(config)) onFontSizeIncrease?.Invoke();
                if (InputService.IsFontSizeDecreasePressed(config)) onFontSizeDecrease?.Invoke();
                if (InputService.IsTabPressed(config)) onTab?.Invoke();
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

            AddLogEntry(formattedLog);
        }

        private void OnScrollChanged(Vector2 _)
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
                    _resizeHandle.Initialize(terminalPanel.GetComponent<RectTransform>());
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
                toggleComponent.onTab = ShowAutoComplete;

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
            // Set the terminal handler reference for DefaultActions
            DefaultActions.SetTerminalHandler(this);
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

        private void ShowAutoComplete()
        {
            if (!_isVisible || !_commandInputField) return;

            // Only show auto-complete if the input field is focused
            if (!_commandInputField.isFocused) return;
            string currentInput = _commandInputField.text.Trim();
            var valueRegistry = TerminalValueRegistry.Instance;

            // All values and actions start with /
            if (!currentInput.StartsWith("/"))
                return;

            string pathInput = currentInput.Substring(1); // Remove / prefix

            // Check if it's an action (has parentheses) or value/action without params (no parentheses)
            bool isAction = pathInput.Contains("(") || pathInput.Contains(")");
            string searchInput = isAction && pathInput.Contains("(")
                ? pathInput.Substring(0, pathInput.IndexOf("(", StringComparison.Ordinal))
                : pathInput;

            // Get all values as strings (combine static and instance, then filter)
            var allValues = valueRegistry.GetAllStaticValues()
                .Concat(valueRegistry.GetAllInstanceValues())
                .Where(v => string.IsNullOrWhiteSpace(searchInput) ||
                            v.StartsWith(searchInput, StringComparison.OrdinalIgnoreCase))
                .Select(v => $"/{v}");

            // Get all actions as strings (combine static and instance, then filter)
            // Show without () if no parameters, with () if it has parameters
            var allActions = valueRegistry.GetAllStaticActionsWithParameters()
                .Select(a => (path: a.actionName, a.parameters))
                .Concat(valueRegistry.GetAllInstanceActionsWithParameters()
                    .Select(a => (path: a.actionPath, a.parameters)))
                .Where(a => string.IsNullOrWhiteSpace(searchInput) ||
                            a.path.StartsWith(searchInput, StringComparison.OrdinalIgnoreCase))
                .Select(a =>
                {
                    if (a.parameters == null || a.parameters.Length == 0)
                        return $"/{a.path}";

                    // Has parameters - show with parentheses
                    var paramString = TerminalHelper.FormatParameters(a.parameters);
                    return $"/{a.path}({paramString})";
                });

            // Combine based on whether we're looking for actions only
            var allMatches = isAction
                ? allActions
                : allValues.Concat(allActions);
            var sortedMatches = allMatches.OrderBy(x => x).ToList();

            switch (sortedMatches.Count)
            {
                case 0:
                    LogDirectly(isAction ? "No matching actions found." : "No matching values or actions found.",
                        LogType.Warning);
                    return;
                // If there's only one match, auto-complete it
                case 1:
                {
                    var singleMatch = sortedMatches[0];
                
                    // For actions with parentheses, remove the parentheses for auto-complete
                    string autoCompleteText = InputParserService.RemoveParenthesesAndParameters(singleMatch);
                
                    _commandInputField.text = autoCompleteText;
                    _commandInputField.MoveToEndOfLine(false, false); // Move cursor to end
                    break;
                }
                default:
                {
                    // Find common prefix up to a "." if all matches share it
                    string commonPrefix = FindCommonPrefixUpToDot(sortedMatches);
                    if (!string.IsNullOrEmpty(commonPrefix))
                    {
                        _commandInputField.text = commonPrefix;
                        _commandInputField.MoveToEndOfLine(false, false); // Move cursor to end
                    }

                    break;
                }
            }

            LogDirectly($"Possible {(isAction ? "actions" : "values and actions")} ({sortedMatches.Count}):",
                LogType.Warning);
            foreach (var match in sortedMatches)
            {
                LogDirectly($"  {match}", LogType.Warning);
            }
        }

        /// <summary>
        /// Finds the common prefix of all matches up to and including a "." if all matches share it.
        /// Returns empty string if no common prefix ending with "." is found.
        /// </summary>
        private string FindCommonPrefixUpToDot(List<string> matches)
        {
            if (matches == null || matches.Count < 2)
                return string.Empty;

            // Remove parentheses and parameters from matches for comparison
            var normalizedMatches = matches
                .Select(InputParserService.RemoveParenthesesAndParameters).ToList();

            // Find the common prefix
            string commonPrefix = normalizedMatches[0];
            for (var i = 1; i < normalizedMatches.Count; i++)
            {
                string current = normalizedMatches[i];
                int minLength = Math.Min(commonPrefix.Length, current.Length);
                int matchLength = 0;

                for (int j = 0; j < minLength; j++)
                {
                    if (commonPrefix[j] == current[j]) matchLength++;
                    else break;
                }

                commonPrefix = commonPrefix.Substring(0, matchLength);
                if (string.IsNullOrEmpty(commonPrefix))
                    return string.Empty;
            }

            // Check if the common prefix ends with a "."
            int lastDotIndex = commonPrefix.LastIndexOf('.');
            return lastDotIndex >= 0 ? commonPrefix.Substring(0, lastDotIndex + 1) : string.Empty;
        }


        private void LogDirectly(string message, LogType logType)
        {
            if (!IsEnabled || !_logTextComponent) return;

            // Format the log entry with color based on type (without parsing tokens)
            string colorCode = TerminalHelper.GetColorForLogType(logType);
            string formattedLog = $"<color={colorCode}>{StartString} {message}</color>";

            AddLogEntry(formattedLog);
        }

        private void AddLogEntry(string formattedLog)
        {
            _logEntries.Enqueue(formattedLog);

            // If we're at max capacity, remove the oldest entry from the builder
            if (_logEntries.Count > _config.maxOnScreenLogEntries)
            {
                _logEntries.Dequeue();
                var removedLength = _logEntryLengths.Dequeue();
                // Remove the first entry from the builder (entry + newline)
                _logBuilder.Remove(0, removedLength);
            }

            // Track the length of this entry (including newline from AppendLine)
            var entryLength = formattedLog.Length + Environment.NewLine.Length;
            _logEntryLengths.Enqueue(entryLength);

            // Append the new entry
            _logBuilder.AppendLine(formattedLog);

            // Update the text component
            _logTextComponent.text = _logBuilder.ToString();

            // Only auto-scroll if user hasn't manually scrolled up
            if (!_scrollRect || !_autoScroll) return;
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ProcessCommand(string command)
        {
            // All commands must start with / for consistency
            if (!command.StartsWith("/"))
            {
                BLogger.Warn($"Unknown command: \"{command}\". All commands must start with '/' (e.g., /help, /clear)");
                return;
            }

            // Try to execute as value or action
            // Validate syntax before processing
            if (!ValidateTokenSyntax(command, out string syntaxError))
            {
                BLogger.Error($"Syntax error: {syntaxError}");
                return;
            }

            // Process tokens and display results through the logger
            // The ParseTerminalTokensAndActions method will handle /values and /actions
            BLogger.Log(command);
        }

        private bool ValidateTokenSyntax(string input, out string error)
        {
            error = null;

            // Check if input starts with /
            if (!input.StartsWith("/")) return true;

            // Check for unmatched parentheses in actions
            int openParens = input.Count(c => c == '(');
            int closeParens = input.Count(c => c == ')');

            if (openParens != closeParens)
            {
                error = "Unmatched parentheses in action syntax. Expected format: /action or /action(args)";
                return false;
            }

            // If no parentheses, it's valid (could be value or action without params)
            if (openParens == 0) return true;

            // Check if action with parentheses is malformed
            var actionRegex = new Regex(@"/[\w\.]+\([^\)]*\)");

            if (actionRegex.IsMatch(input)) return true;
            error =
                $"Failed to Parse: {input} Actions with parameters require parentheses. Expected format: /action(args)";
            return false;
        }

        public void ClearLogs()
        {
            _logEntries.Clear();
            _logEntryLengths.Clear();
            _logBuilder.Clear();
            var clearedMessage = $"<color=green>{StartString} Console cleared</color>";
            _logEntries.Enqueue(clearedMessage);
            _logEntryLengths.Enqueue(clearedMessage.Length + Environment.NewLine.Length);
            _logBuilder.AppendLine(clearedMessage);
            _logTextComponent.text = _logBuilder.ToString();
        }

        public TextMeshProUGUI GetLogTextComponent()
        {
            return _logTextComponent;
        }

        public void CloseTerminal()
        {
            if (!_terminalInstance) return;

            _isVisible = false;
            _terminalInstance.SetActive(false);

            Debug.Log("[TerminalHandler] Terminal closed");
        }

        public void Shutdown()
        {
            // Clean up event listeners
            if (_commandInputField) _commandInputField.onSubmit.RemoveAllListeners();
            if (_sendButton) _sendButton.onClick.RemoveAllListeners();
            if (_scrollRect) _scrollRect.onValueChanged.RemoveAllListeners();
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
            _logEntryLengths.Clear();
            _logBuilder.Clear();
            IsEnabled = false;

            Debug.Log("[TerminalHandler] Shutdown complete.");
        }

        public bool IsEnabled { get; private set; }

        private string ParseTerminalTokensAndActions(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Don't parse if input already contains HTML tags (already formatted)
            if (input.Contains("<color") || input.Contains("</color>")) return input;

            // Handle / prefix for both values and actions
            if (!input.StartsWith("/")) return input;

            // Extract action calls manually to handle nested parentheses correctly
            // The regex approach doesn't handle nested parens well, so we'll do it manually
            var result = new StringBuilder();
            int i = 0;

            while (i < input.Length)
            {
                if (input[i] == '/' && (i == 0 || input[i - 1] != '<'))
                {
                    // Try to extract an action call
                    int pathStart = i + 1;
                    int pathEnd = pathStart;

                    // Find the path (word characters, dots, underscores)
                    while (pathEnd < input.Length && 
                           (char.IsLetterOrDigit(input[pathEnd]) || input[pathEnd] == '_' || input[pathEnd] == '.'))
                    {
                        pathEnd++;
                    }

                    if (pathEnd > pathStart && pathEnd < input.Length && input[pathEnd] == '(')
                    {
                        // Found an action with parentheses - extract everything up to the last )
                        int openParen = pathEnd;
                        int closeParen = input.LastIndexOf(')');
                        
                        if (closeParen > openParen)
                        {
                            string actionToken = input.Substring(pathStart, closeParen - pathStart + 1);
                            
                            if (TerminalValueRegistry.Instance.TryExecuteAction(actionToken, out var actionResult))
                            {
                                var colorizedAction = ColorizeActionPath(actionToken);
                                if (actionResult == null)
                                    result.Append($"<color=red>Executed action: </color> {colorizedAction} [Action executed]");
                                else
                                {
                                    var resultStr = TerminalHelper.FormatValue(actionResult);
                                    resultStr = resultStr.Replace("<", "&lt;").Replace(">", "&gt;");
                                    result.Append($"<color=red>Executed action: </color> {colorizedAction} [Returned: {resultStr}]");
                                }
                                i = closeParen + 1;
                                continue;
                            }
                            else
                            {
                                result.Append($"<color=red>Unknown action: \"/{actionToken}\"</color>");
                                i = closeParen + 1;
                                continue;
                            }
                        }
                    }
                    else if (pathEnd > pathStart)
                    {
                        // Simple token without parentheses
                        string token = input.Substring(pathStart, pathEnd - pathStart);
                        string replacement = ProcessActionOrValue(token);
                        result.Append(replacement);
                        i = pathEnd;
                        continue;
                    }
                }

                result.Append(input[i]);
                i++;
            }

            input = result.ToString();

            return input;
        }

        private string ProcessActionOrValue(string token)
        {
            // Try as action first (actions without parameters)
            if (TerminalValueRegistry.Instance.TryExecuteAction(token, out var result))
            {
                var colorizedAction = ColorizeActionPath(token);
                if (result == null)
                    return $"<color=red>Executed action: </color> {colorizedAction} [Action executed]";

                var resultStr = TerminalHelper.FormatValue(result);
                resultStr = resultStr.Replace("<", "&lt;").Replace(">", "&gt;");
                return $"<color=red>Executed action: </color> {colorizedAction} [Returned: {resultStr}]";
            }

            // Try as value
            if (!TerminalValueRegistry.Instance.TryGetValue(token, out var value))
                return $"<color=red>Unknown token: /{token}</color>";

            var valueStr = TerminalHelper.FormatValue(value);
            valueStr = valueStr.Replace("<", "&lt;").Replace(">", "&gt;");
            var colorizedToken = TerminalHelper.ColorizeValuePath(token);
            return $"<color=red>/</color>{colorizedToken}<color=white>=</color>{valueStr}";
        }


        private static string ColorizeActionPath(string actionToken)
        {
            // Extract a path and arguments: heal(50) or Players.player1.heal(50) or clear
            var openParen = actionToken.IndexOf('(');
            string path;
            string argsWithParen = "";

            if (openParen == -1)
            {
                // No parentheses - action without parameters
                path = actionToken;
            }
            else
            {
                path = actionToken.Substring(0, openParen);
                argsWithParen = actionToken.Substring(openParen); // (50)
            }

            var parts = path.Split('.');

            return parts.Length switch
            {
                1 => $"<color=white>{parts[0]}</color><color=grey>{argsWithParen}</color>",
                3 =>
                    $"<color=red>{parts[0]}</color>.<color=#ADD8E6>{parts[1]}</color>.<color=white>{parts[2]}</color><color=grey>{argsWithParen}</color>",
                _ => $"<color=white>{path}</color><color=grey>{argsWithParen}</color>"
            };
        }
    }
}