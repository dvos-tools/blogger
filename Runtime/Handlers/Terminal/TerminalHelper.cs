using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

        /// <summary>
        /// Formats a value for display in the terminal, handling collections, dictionaries, and complex types.
        /// </summary>
        public static string FormatValue(object value, int maxDepth = 2, int maxItems = 50)
        {
            if (value == null) return "null";

            var type = value.GetType();

            // Handle dictionaries
            if (IsDictionaryType(type)) return FormatDictionary(value, maxDepth, maxItems);
            if (IsCollectionType(type)) return FormatCollection(value, maxDepth, maxItems);
            if (IsBasicType(type)) return value.ToString();

            // For complex objects, try to show a meaningful representation
            // If it has a custom ToString() that's not just the type name, use it
            var toStringResult = value.ToString();
            if (toStringResult != type.FullName && toStringResult != type.Name) return toStringResult;

            // Otherwise, show properties if it's a simple object
            return maxDepth > 0 ? FormatObject(value, maxDepth - 1, maxItems) : toStringResult;
        }

        private static bool IsDictionaryType(Type type)
        {
            // Check if it implements IDictionary or IDictionary<TKey, TValue>
            if (type == null) return false;
            if (typeof(IDictionary).IsAssignableFrom(type)) return true;

            var interfaces = type.GetInterfaces();
            return interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        private static bool IsCollectionType(Type type)
        {
            // Check if it implements IEnumerable (but not string, which is also IEnumerable<char>)
            if (type == null) return false;
            if (type.IsArray) return true;
            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static bool IsBasicType(Type type)
        {
            if (type == null) return false;
            if (type.IsPrimitive) return true;
            if (type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || 
                type == typeof(TimeSpan) || type == typeof(Guid))
                return true;

            // Nullable value types
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)) return type.IsEnum;
            var underlyingType = Nullable.GetUnderlyingType(type);
            return IsBasicType(underlyingType);
        }

        private static string FormatDictionary(object dict, int maxDepth, int maxItems)
        {
            var sb = new StringBuilder();
            sb.Append("{");

            try
            {
                var items = new List<string>();
                int count = 0;

                if (dict is IDictionary nonGenericDict)
                {
                    foreach (DictionaryEntry entry in nonGenericDict)
                    {
                        if (count >= maxItems)
                        {
                            items.Add("...");
                            break;
                        }

                        var keyStr = FormatValue(entry.Key, maxDepth - 1, maxItems);
                        var valueStr = FormatValue(entry.Value, maxDepth - 1, maxItems);
                        items.Add($"{keyStr}: {valueStr}");
                        count++;
                    }
                }
                else
                {
                    // Handle generic dictionaries via reflection
                    var getEnumeratorMethod = dict.GetType().GetMethod("GetEnumerator");
                    if (getEnumeratorMethod != null)
                    {
                        var enumerator = getEnumeratorMethod.Invoke(dict, null);
                        var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                        var currentProperty = enumerator.GetType().GetProperty("Current");
                        var keyProperty = currentProperty?.PropertyType.GetProperty("Key");
                        var valueProperty = currentProperty?.PropertyType.GetProperty("Value");

                        if (moveNextMethod != null && keyProperty != null && valueProperty != null)
                        {
                            while ((bool)moveNextMethod.Invoke(enumerator, null))
                            {
                                if (count >= maxItems)
                                {
                                    items.Add("...");
                                    break;
                                }

                                var current = currentProperty.GetValue(enumerator);
                                var key = keyProperty.GetValue(current);
                                var value = valueProperty.GetValue(current);

                                var keyStr = FormatValue(key, maxDepth - 1, maxItems);
                                var valueStr = FormatValue(value, maxDepth - 1, maxItems);
                                items.Add($"{keyStr}: {valueStr}");
                                count++;
                            }
                        }
                    }
                }

                sb.Append(string.Join(", ", items));
            }
            catch (Exception ex)
            {
                sb.Append($"<error formatting dictionary: {ex.Message}>");
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string FormatCollection(object collection, int maxDepth, int maxItems)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            try
            {
                var items = new List<string>();
                int count = 0;

                if (collection is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (count >= maxItems)
                        {
                            items.Add("...");
                            break;
                        }

                        items.Add(FormatValue(item, maxDepth - 1, maxItems));
                        count++;
                    }
                }

                sb.Append(string.Join(", ", items));
            }
            catch (Exception ex)
            {
                sb.Append($"<error formatting collection: {ex.Message}>");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private static string FormatObject(object obj, int maxDepth, int maxItems)
        {
            var type = obj.GetType();
            var sb = new StringBuilder();
            sb.Append($"{type.Name} {{");

            try
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Take(maxItems)
                    .ToList();

                var propertyStrings = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        var propValue = prop.GetValue(obj);
                        var valueStr = FormatValue(propValue, maxDepth - 1, maxItems);
                        propertyStrings.Add($"{prop.Name}: {valueStr}");
                    }
                    catch
                    {
                        propertyStrings.Add($"{prop.Name}: <error>");
                    }
                }

                if (propertyStrings.Count > 0)
                {
                    sb.Append(" ");
                    sb.Append(string.Join(", ", propertyStrings));
                    sb.Append(" ");
                }
            }
            catch (Exception ex)
            {
                sb.Append($" <error formatting object: {ex.Message}> ");
            }

            sb.Append("}");
            return sb.ToString();
        }
    }
}