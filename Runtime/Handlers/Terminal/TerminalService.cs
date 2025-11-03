using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using com.DvosTools.blogger.Attributes;
using UnityEngine;

namespace com.DvosTools.blogger.Service
{
    internal static class TerminalService
    {
        public static string GetInstanceKey(object instance)
        {
            var type = instance.GetType();
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Check fields
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.GetCustomAttribute<BLoggerAggregateIdAttribute>() == null) continue;
                var value = field.GetValue(instance);
                return value?.ToString();
            }

            // Check properties
            return (from property in type.GetProperties(bindingFlags)
                where property.GetCustomAttribute<BLoggerAggregateIdAttribute>() != null && property.CanRead
                select property.GetValue(instance)
                into value
                select value?.ToString()).FirstOrDefault();
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

        public static string ColorizeValuePath(string token)
        {
            var parts = token.Split('.');

            return parts.Length switch
            {
                1 => $"<color=white>{parts[0]}</color>",
                3 => $"<color=red>{parts[0]}</color>.<color=#ADD8E6>{parts[1]}</color>.<color=white>{parts[2]}</color>",
                _ => $"<color=white>{token}</color>"
            };
        } 
        
        public static string GetColorForLogType(LogType type)
        {
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
    }
}