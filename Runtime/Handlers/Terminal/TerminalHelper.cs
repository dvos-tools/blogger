using System.Linq;
using System.Reflection;
using com.DvosTools.blogger.Attributes;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers.Terminal
{
    internal static class TerminalHelper
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

        public static string FormatParameters(ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return "";
            
            return string.Join(", ", parameters.Select(p => p.ParameterType.Name));
        }
        
    }
}