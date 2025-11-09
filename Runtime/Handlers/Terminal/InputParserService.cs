using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace com.DvosTools.blogger.Handlers.Terminal
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
        /// Examples: "heal(50)", "Players.player1.heal(50)", "pause(true)", "kill()", "clear"
        /// </summary>
        public static ActionCall ParseActionCall(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var openParen = input.IndexOf('(');
            var closeParen = input.LastIndexOf(')');

            // If no parentheses, treat as action with no parameters
            if (openParen == -1 || closeParen == -1 || closeParen <= openParen)
            {
                return new ActionCall
                {
                    Path = input,
                    Arguments = Array.Empty<string>()
                };
            }

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
        
        public static string[] ParseCommandLine(string commandLine)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            foreach (var c in commandLine)
            {
                if (c == '"') 
                    inQuotes = !inQuotes;
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length <= 0) continue;
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
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


        public static object TryParseUnityType(string rawArg, Type targetType)
        {
            if (targetType == typeof(Vector2))
                return TryParseVector2(rawArg);

            if (targetType == typeof(Vector3))
                return TryParseVector3(rawArg);

            return null;
        }
        
        public static Vector2? TryParseVector2(string rawArg)
        {
            var parts = rawArg.Split(',');
            if (parts.Length == 2 && 
                float.TryParse(parts[0].Trim(), out var x) && 
                float.TryParse(parts[1].Trim(), out var y))
            {
                return new Vector2(x, y);
            }
            return null;
        }

        public static Vector3? TryParseVector3(string rawArg)
        {
            var parts = rawArg.Split(',');
            if (parts.Length == 3 && 
                float.TryParse(parts[0].Trim(), out var x) && 
                float.TryParse(parts[1].Trim(), out var y) && 
                float.TryParse(parts[2].Trim(), out var z))
            {
                return new Vector3(x, y, z);
            }
            return null;
        }
        
        public static string StripQuotes(string arg)
        {
            if (arg.StartsWith("\"") && arg.EndsWith("\"") && arg.Length >= 2)
            {
                return arg.Substring(1, arg.Length - 2);
            }
            return arg;
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
