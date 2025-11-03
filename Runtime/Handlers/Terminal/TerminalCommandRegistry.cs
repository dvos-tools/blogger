using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Registry for terminal commands with their metadata and execution logic
    /// </summary>
    public class TerminalCommandRegistry
    {
        private static TerminalCommandRegistry _instance;
        public static TerminalCommandRegistry Instance => _instance ??= new TerminalCommandRegistry();

        private readonly Dictionary<string, TerminalCommand> _commands = new();

        public class TerminalCommand
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Usage { get; set; }
            public string[] Aliases { get; set; }
            public Action<string[]> ExecuteAction { get; set; }
            public bool IsHidden { get; set; }

            public TerminalCommand(
                string name,
                string description,
                string usage,
                Action<string[]> executeAction,
                string[] aliases = null,
                bool isHidden = false)
            {
                Name = name;
                Description = description;
                Usage = usage;
                ExecuteAction = executeAction;
                Aliases = aliases ?? Array.Empty<string>();
                IsHidden = isHidden;
            }
        }

        private TerminalCommandRegistry()
        {
        }

        /// <summary>
        /// Register a command with the terminal
        /// </summary>
        public void RegisterCommand(TerminalCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Command name cannot be null or empty");

            var lowerName = command.Name.ToLower();
            if (_commands.ContainsKey(lowerName))
                UnityEngine.Debug.LogWarning(
                    $"[TerminalCommandRegistry] Command '{command.Name}' already registered. Overwriting.");

            _commands[lowerName] = command;
            if (command.Aliases != null)
                foreach (var alias in command.Aliases)
                    _commands[alias.ToLower()] = command;
        }

        /// <summary>
        /// Try to execute a command by name
        /// </summary>
        public bool TryExecuteCommand(string commandLine, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(commandLine))
                return false;

            var parts = TerminalService.ParseCommandLine(commandLine);
            if (parts.Length == 0)
                return false;

            var commandName = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    command.ExecuteAction?.Invoke(args);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error executing command '{commandName}': {ex.Message}";
                    return false;
                }
            }

            errorMessage = $"Not found \"{commandName}\"";
            return false;
        }

        /// <summary>
        /// Get all registered commands (excluding hidden and aliases)
        /// </summary>
        public IEnumerable<TerminalCommand> GetAllCommands()
        {
            return _commands.Values
                .Where(c => !c.IsHidden)
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .OrderBy(c => c.Name);
        }

        /// <summary>
        /// Generate help text for all commands
        /// </summary>
        public string GenerateHelpText()
        {
            var helpText = new StringBuilder();
            helpText.AppendLine("Available Commands:");
            helpText.AppendLine("");

            var commands = GetAllCommands().ToList();

            if (commands.Count == 0)
            {
                helpText.AppendLine("  No commands registered.");
                return helpText.ToString();
            }

            // Find the longest command name for alignment
            int maxNameLength = commands.Max(c => c.Name.Length);

            foreach (var command in commands)
            {
                var padding = new string(' ', maxNameLength - command.Name.Length + 2);
                helpText.AppendLine($"  {command.Name}{padding}- {command.Description}");

                if (!string.IsNullOrWhiteSpace(command.Usage))
                    helpText.AppendLine($"    Usage: {command.Usage}");

                if (command.Aliases != null && command.Aliases.Length > 0)
                    helpText.AppendLine($"    Aliases: {string.Join(", ", command.Aliases)}");
            }

            helpText.AppendLine("");
            helpText.AppendLine("Terminal Values (@):");
            helpText.AppendLine("  @valueName               - Display a static value");
            helpText.AppendLine("  @Aggregate.key.value     - Display an instance value");
            helpText.AppendLine("");
            helpText.AppendLine("Terminal Actions (!):");
            helpText.AppendLine("  !actionName()            - Execute a static action");
            helpText.AppendLine("  !Aggregate.key.action(args) - Execute an instance action");
            helpText.AppendLine("");
            helpText.AppendLine("Examples:");
            helpText.AppendLine("  @fps                     - Display FPS value");
            helpText.AppendLine("  !pause(true)             - Pause the game");

            return helpText.ToString();
        }

        /// <summary>
        /// Generate help text for a specific command
        /// </summary>
        public string GenerateCommandHelp(string commandName)
        {
            if (!_commands.TryGetValue(commandName.ToLower(), out var command))
                return $"Not found \"{commandName}\"";

            var helpText = new StringBuilder();
            helpText.AppendLine($"Command: {command.Name}");
            helpText.AppendLine($"Description: {command.Description}");

            if (!string.IsNullOrWhiteSpace(command.Usage))
                helpText.AppendLine($"Usage: {command.Usage}");

            if (command.Aliases != null && command.Aliases.Length > 0)
                helpText.AppendLine($"Aliases: {string.Join(", ", command.Aliases)}");

            return helpText.ToString();
        }

      
    }
}