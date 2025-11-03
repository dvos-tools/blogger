using System;
using System.Collections.Generic;
using System.Text;

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Static service for parsing terminal input commands and action calls
    /// </summary>
    public static class InputParserService
    {
        /// <summary>
        /// Represents a parsed action call with a path and arguments
        /// </summary>
        public class ActionCall
        {
            public string Path { get; set; }
            public string[] Arguments { get; set; }
        }

        /// <summary>
        /// Parses an action call string into its components.
        /// Examples: "heal(50)", "Players.player1.heal(50)", "pause(true)", "kill()"
        /// </summary>
        public static ActionCall ParseActionCall(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var openParen = input.IndexOf('(');
            var closeParen = input.LastIndexOf(')');

            if (openParen == -1 || closeParen == -1 || closeParen <= openParen)
                return null;

            var path = input.Substring(0, openParen);
            var argsString = input.Substring(openParen + 1, closeParen - openParen - 1).Trim();

            var arguments = string.IsNullOrEmpty(argsString) 
                ? Array.Empty<string>() 
                : SplitArguments(argsString);

            return new ActionCall
            {
                Path = path,
                Arguments = arguments
            };
        }

        /// <summary>
        /// Splits comma-separated arguments respecting quotes and nested brackets/parentheses.
        /// For more robust parsing, consider using Microsoft.CodeAnalysis.CSharp (Roslyn) or a regex solution.
        /// </summary>
        private static string[] SplitArguments(string argsString)
        {
            if (string.IsNullOrEmpty(argsString))
                return Array.Empty<string>();

            var args = new List<string>();
            var current = new StringBuilder();
            var state = new ParserState();

            foreach (var c in argsString)
            {
                if (c == '"' && !state.IsEscaped)
                {
                    state.InQuotes = !state.InQuotes;
                    current.Append(c);
                }
                else if (c == ',' && state.CanSplit)
                {
                    AddArgument(args, current);
                }
                else
                {
                    if (!state.InQuotes)
                        state.UpdateDepth(c);
                    
                    current.Append(c);
                }

                state.IsEscaped = c == '\\' && !state.IsEscaped;
            }

            AddArgument(args, current);
            return args.ToArray();
        }

        private static void AddArgument(List<string> args, StringBuilder current)
        {
            if (current.Length > 0)
            {
                args.Add(current.ToString().Trim());
                current.Clear();
            }
        }

        private class ParserState
        {
            public bool InQuotes { get; set; }
            public bool IsEscaped { get; set; }
            private int Depth { get; set; }
            public bool CanSplit => !InQuotes && Depth == 0;

            public void UpdateDepth(char c)
            {
                Depth += c switch
                {
                    '(' or '[' or '{' => 1,
                    ')' or ']' or '}' => -1,
                    _ => 0
                };
            }
        }
    }
}
