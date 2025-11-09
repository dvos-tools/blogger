using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using com.DvosTools.blogger.Attributes;
using com.DvosTools.blogger.Context;
using TMPro;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers.Terminal
{
    /// <summary>
    /// Default actions available in the terminal.
    /// These are automatically discovered via BLoggerActionAttribute.
    /// </summary>
    public static class DefaultActions
    {
        // Reference to TerminalHandler for accessing terminal state
        private static TerminalHandler _terminalHandler;
        
        public static void SetTerminalHandler(TerminalHandler handler)
        {
            _terminalHandler = handler;
        }

        [BLoggerAction("clear")]
        public static void Clear()
        {
            _terminalHandler?.ClearLogs();
        }

        [BLoggerAction("copy")]
        public static void Copy()
        {
            if (_terminalHandler == null) return;
            
            var logTextComponent = _terminalHandler.GetLogTextComponent();
            if (logTextComponent == null) return;
            
            // Remove rich text tags for clean copy
            var textToCopy = Regex.Replace(logTextComponent.text, "<.*?>", string.Empty);
            GUIUtility.systemCopyBuffer = textToCopy;
            BLogger.Log("Terminal output copied to clipboard!");
        }

        [BLoggerAction("context")]
        public static void Context()
        {
            var context = LoggingContext.GetFormattedContext();
            BLogger.Log($"Logging Context: [{context}]");
        }

        [BLoggerAction("exit")]
        public static void Exit()
        {
            _terminalHandler?.CloseTerminal();
        }

        [BLoggerAction("quit")]
        public static void Quit()
        {
            Exit(); // Alias for exit
        }

        [BLoggerAction("help")]
        public static void Help()
        {
            GenerateGeneralHelp();
        }

        private static void GenerateGeneralHelp()
        {
            var helpText = new StringBuilder();
            helpText.AppendLine("Available Actions:");
            helpText.AppendLine("");
            
            var valueRegistry = TerminalValueRegistry.Instance;
            var allStaticActions = valueRegistry.GetAllStaticActionsWithParameters().ToList();
            var allInstanceActions = valueRegistry.GetAllInstanceActionsWithParameters().ToList();
            
            // Get all static actions (including default actions)
            var staticActions = allStaticActions
                .OrderBy(a => a.actionName)
                .ToList();
            
            if (staticActions.Count > 0)
            {
                helpText.AppendLine("Static Actions:");
                foreach (var action in staticActions)
                {
                    if (action.parameters == null || action.parameters.Length == 0)
                    {
                        helpText.AppendLine($"  /{action.actionName}");
                    }
                    else
                    {
                        var paramString = TerminalHelper.FormatParameters(action.parameters);
                        helpText.AppendLine($"  /{action.actionName}({paramString})");
                    }
                }
                helpText.AppendLine("");
            }
            
            // Get all instance actions
            var instanceActions = allInstanceActions
                .OrderBy(a => a.actionPath)
                .ToList();
            
            if (instanceActions.Count > 0)
            {
                helpText.AppendLine("Instance Actions:");
                foreach (var action in instanceActions)
                {
                    if (action.parameters == null || action.parameters.Length == 0)
                    {
                        helpText.AppendLine($"  /{action.actionPath}");
                    }
                    else
                    {
                        var paramString = TerminalHelper.FormatParameters(action.parameters);
                        helpText.AppendLine($"  /{action.actionPath}({paramString})");
                    }
                }
                helpText.AppendLine("");
            }
            
            // Get all values
            var allStaticValues = valueRegistry.GetAllStaticValues().ToList();
            var allInstanceValues = valueRegistry.GetAllInstanceValues().ToList();
            
            if (allStaticValues.Count > 0 || allInstanceValues.Count > 0)
            {
                helpText.AppendLine("Values:");
                foreach (var value in allStaticValues.OrderBy(v => v))
                {
                    helpText.AppendLine($"  /{value}");
                }
                foreach (var value in allInstanceValues.OrderBy(v => v))
                {
                    helpText.AppendLine($"  /{value}");
                }
                helpText.AppendLine("");
            }
            
            helpText.AppendLine("Examples:");
            helpText.AppendLine("  /clear                    - Clear the console");
            helpText.AppendLine("  /copy                      - Copy terminal output to clipboard");
            helpText.AppendLine("  /context                   - Show current logging context");
            helpText.AppendLine("  /exit                      - Close the terminal");
            helpText.AppendLine("  /Player.test.health        - Display instance value");
            helpText.AppendLine("  /Player.test.setHealth(100) - Execute instance action");
            
            BLogger.Log(helpText.ToString());
        }
    }
}
