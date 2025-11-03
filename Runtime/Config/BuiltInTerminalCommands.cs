using System;
using System.Collections.Generic;

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Wrapper for command arguments with typed access
    /// </summary>
    public class CommandArgs
    {
        private readonly string[] _args;

        public CommandArgs(string[] args)
        {
            _args = args ?? Array.Empty<string>();
        }

        public int Count => _args.Length;
        public bool HasArgs => _args.Length > 0;
        
        public string Get(int index) => index < _args.Length ? _args[index] : null;
        public string GetOrDefault(int index, string defaultValue) => index < _args.Length ? _args[index] : defaultValue;
        
        public string[] ToArray() => _args;
    }

    /// <summary>
    /// Defines all built-in terminal commands
    /// </summary>
    public static class BuiltInTerminalCommands
    {
        public enum Command
        {
            Clear,
            Help,
            Copy,
            Context,
            Exit
        }

        /// <summary>
        /// Get all built-in command definitions
        /// </summary>
        public static Dictionary<Command, CommandDefinition> GetCommandDefinitions(
            Action<CommandArgs> clearAction,
            Action<CommandArgs> helpAction,
            Action<CommandArgs> copyAction,
            Action<CommandArgs> contextAction,
            Action<CommandArgs> exitAction)
        {
            return new Dictionary<Command, CommandDefinition>
            {
                [Command.Clear] = new CommandDefinition
                {
                    Name = "clear",
                    Description = "Clear the console",
                    Usage = "clear",
                    Aliases = new[] { "cls" },
                    ExecuteAction = clearAction
                },
                [Command.Help] = new CommandDefinition
                {
                    Name = "help",
                    Description = "Show this help message",
                    Usage = "help [command]",
                    Aliases = new[] { "?", "h" },
                    ExecuteAction = helpAction
                },
                [Command.Copy] = new CommandDefinition
                {
                    Name = "copy",
                    Description = "Copy terminal output to clipboard",
                    Usage = "copy",
                    Aliases = new[] { "c" },
                    ExecuteAction = copyAction
                },
                [Command.Context] = new CommandDefinition
                {
                    Name = "context",
                    Description = "Show current logging context",
                    Usage = "context",
                    Aliases = new[] { "ctx" },
                    ExecuteAction = contextAction
                },
                [Command.Exit] = new CommandDefinition
                {
                    Name = "exit",
                    Description = "Close the terminal window",
                    Usage = "exit",
                    Aliases = new[] { "quit", "q" },
                    ExecuteAction = exitAction
                }
            };
        }

        public class CommandDefinition
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Usage { get; set; }
            public string[] Aliases { get; set; }
            public Action<CommandArgs> ExecuteAction { get; set; }
        }
    }
}
