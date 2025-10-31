using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using com.DvosTools.blogger.Attributes;

namespace com.DvosTools.blogger.Service
{
    public class TerminalValueRegistry
    {
        private static TerminalValueRegistry _instance;
        public static TerminalValueRegistry Instance => _instance ??= new TerminalValueRegistry();

        private readonly Dictionary<string, Func<object>> _staticValues = new();
        private readonly Dictionary<(string aggregateName, string instanceKey, string valueName), Func<object>> _instanceValues = new();
        private readonly Dictionary<string, ActionInvoker> _staticActions = new();
        private readonly Dictionary<(string aggregateName, string instanceKey, string actionName), ActionInvoker> _instanceActions = new();
        private readonly HashSet<Type> _aggregateTypes = new();
        private readonly Dictionary<(string aggregateName, string instanceKey), WeakReference> _registeredInstances = new();

        private class ActionInvoker
        {
            public MethodInfo Method { get; set; }
            public ParameterInfo[] Parameters { get; set; }
            public object Target { get; set; }

            public object Invoke(object[] args)
            {
                return Method.Invoke(Target, args);
            }
        }

        private TerminalValueRegistry()
        {
            ScanAssembliesForTerminalValues();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            var _ = Instance;
            Debug.Log("[TerminalValueRegistry] Initialized and scanned assemblies");
        }

        private void ScanAssembliesForTerminalValues()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        if (type.GetCustomAttribute<BLoggerAggregateAttribute>() != null)
                        {
                            _aggregateTypes.Add(type);
                        }

                        ScanStaticMembers(type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }

            Debug.Log($"[TerminalValueRegistry] Found {_staticValues.Count} static values, {_staticActions.Count} static actions, and {_aggregateTypes.Count} aggregate types");
        }

        private void ScanStaticMembers(Type type)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Static;

            foreach (var field in type.GetFields(bindingFlags))
            {
                var attr = field.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null)
                {
                    _staticValues[attr.Name] = () => field.GetValue(null);
                }
            }

            foreach (var property in type.GetProperties(bindingFlags))
            {
                var attr = property.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null && property.CanRead)
                {
                    _staticValues[attr.Name] = () => property.GetValue(null);
                }
            }

            foreach (var method in type.GetMethods(bindingFlags))
            {
                var valueAttr = method.GetCustomAttribute<BLoggerValueAttribute>();
                if (valueAttr != null && method.GetParameters().Length == 0)
                {
                    _staticValues[valueAttr.Name] = () => method.Invoke(null, null);
                }

                var actionAttr = method.GetCustomAttribute<BLoggerActionAttribute>();
                if (actionAttr != null)
                {
                    _staticActions[actionAttr.ActionName] = new ActionInvoker
                    {
                        Method = method,
                        Parameters = method.GetParameters(),
                        Target = null
                    };
                }
            }
        }

        public void RegisterInstance(MonoBehaviour instance)
        {
            var type = instance.GetType();
            
            if (!_aggregateTypes.Contains(type))
                return;

            var aggregateAttr = type.GetCustomAttribute<BLoggerAggregateAttribute>();
            if (aggregateAttr == null)
                return;

            var aggregateName = aggregateAttr.AggregateName;

            // Find the field or property marked with [BLoggerAggregateId]
            var instanceKey = GetInstanceKey(instance);
            if (string.IsNullOrEmpty(instanceKey))
            {
                Debug.LogWarning($"[TerminalValueRegistry] Instance of type {type.Name} has no [BLoggerAggregateId] field/property defined");
                return;
            }

            var fullKey = (aggregateName, instanceKey);

            if (_registeredInstances.ContainsKey(fullKey))
            {
                var existing = _registeredInstances[fullKey];
                if (existing.IsAlive && existing.Target != null)
                {
                    Debug.LogWarning($"[TerminalValueRegistry] Instance '{aggregateName}.{instanceKey}' already registered. Overwriting.");
                }
            }

            _registeredInstances[fullKey] = new WeakReference(instance);

            ScanInstanceMembers(instance, aggregateName, instanceKey);

            Debug.Log($"[TerminalValueRegistry] Registered instance '{aggregateName}.{instanceKey}' of type {type.Name}");
        }

        private string GetInstanceKey(MonoBehaviour instance)
        {
            var type = instance.GetType();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // Check fields
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.GetCustomAttribute<BLoggerAggregateIdAttribute>() != null)
                {
                    var value = field.GetValue(instance);
                    return value?.ToString();
                }
            }

            // Check properties
            foreach (var property in type.GetProperties(bindingFlags))
            {
                if (property.GetCustomAttribute<BLoggerAggregateIdAttribute>() != null && property.CanRead)
                {
                    var value = property.GetValue(instance);
                    return value?.ToString();
                }
            }

            return null;
        }

        private void ScanInstanceMembers(MonoBehaviour instance, string aggregateName, string instanceKey)
        {
            var type = instance.GetType();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            foreach (var field in type.GetFields(bindingFlags))
            {
                var attr = field.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null)
                {
                    var key = (aggregateName, instanceKey, attr.Name);
                    _instanceValues[key] = () => field.GetValue(instance);
                }
            }

            foreach (var property in type.GetProperties(bindingFlags))
            {
                var attr = property.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null && property.CanRead)
                {
                    var key = (aggregateName, instanceKey, attr.Name);
                    _instanceValues[key] = () => property.GetValue(instance);
                }
            }

            foreach (var method in type.GetMethods(bindingFlags))
            {
                var valueAttr = method.GetCustomAttribute<BLoggerValueAttribute>();
                if (valueAttr != null && method.GetParameters().Length == 0)
                {
                    var key = (aggregateName, instanceKey, valueAttr.Name);
                    _instanceValues[key] = () => method.Invoke(instance, null);
                }

                var actionAttr = method.GetCustomAttribute<BLoggerActionAttribute>();
                if (actionAttr != null)
                {
                    var key = (aggregateName, instanceKey, actionAttr.ActionName);
                    _instanceActions[key] = new ActionInvoker
                    {
                        Method = method,
                        Parameters = method.GetParameters(),
                        Target = instance
                    };
                }
            }
        }

        public void UnregisterInstance(MonoBehaviour instance)
        {
            var type = instance.GetType();
            var aggregateAttr = type.GetCustomAttribute<BLoggerAggregateAttribute>();
            
            if (aggregateAttr == null)
                return;

            var aggregateName = aggregateAttr.AggregateName;
            var instanceKey = GetInstanceKey(instance);

            if (string.IsNullOrEmpty(instanceKey))
                return;

            var fullKey = (aggregateName, instanceKey);
            _registeredInstances.Remove(fullKey);

            var valuesToRemove = _instanceValues.Keys
                .Where(k => k.aggregateName == aggregateName && k.instanceKey == instanceKey)
                .ToList();

            foreach (var key in valuesToRemove)
            {
                _instanceValues.Remove(key);
            }

            var actionsToRemove = _instanceActions.Keys
                .Where(k => k.aggregateName == aggregateName && k.instanceKey == instanceKey)
                .ToList();

            foreach (var key in actionsToRemove)
            {
                _instanceActions.Remove(key);
            }

            Debug.Log($"[TerminalValueRegistry] Unregistered instance '{aggregateName}.{instanceKey}'");
        }

        public bool TryGetValue(string token, out object value)
        {
            value = null;

            var parts = token.Split('.');
            
            // Static value: @fps
            if (parts.Length == 1)
            {
                if (_staticValues.TryGetValue(token, out var accessor))
                {
                    try
                    {
                        value = accessor();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[TerminalValueRegistry] Error accessing @{token}: {ex.Message}");
                        return false;
                    }
                }
            }
            // Instance value: @AggregateName.instanceKey.valueName
            else if (parts.Length == 3)
            {
                var aggregateName = parts[0];
                var instanceKey = parts[1];
                var valueName = parts[2];

                var key = (aggregateName, instanceKey, valueName);
                if (_instanceValues.TryGetValue(key, out var accessor))
                {
                    try
                    {
                        value = accessor();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[TerminalValueRegistry] Error accessing @{token}: {ex.Message}");
                        return false;
                    }
                }
            }

            return false;
        }

        public bool IsAggregateType(Type type)
        {
            return _aggregateTypes.Contains(type);
        }

        public bool TryExecuteAction(string actionToken, out object result)
        {
            result = null;

            try
            {
                var parsed = ParseActionCall(actionToken);
                if (parsed == null)
                    return false;

                ActionInvoker invoker = null;

                // Check if it's a static action or instance action
                var pathParts = parsed.Path.Split('.');
                
                if (pathParts.Length == 1)
                {
                    // Static action: !pause(true)
                    if (!_staticActions.TryGetValue(pathParts[0], out invoker))
                    {
                        Debug.LogError($"[TerminalValueRegistry] Unknown static action: {pathParts[0]}");
                        return false;
                    }
                }
                else if (pathParts.Length == 3)
                {
                    // Instance action: !Players.player1.heal(50)
                    var aggregateName = pathParts[0];
                    var instanceKey = pathParts[1];
                    var actionName = pathParts[2];

                    var key = (aggregateName, instanceKey, actionName);
                    if (!_instanceActions.TryGetValue(key, out invoker))
                    {
                        Debug.LogError($"[TerminalValueRegistry] Unknown instance action: {parsed.Path}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"[TerminalValueRegistry] Invalid action path: {parsed.Path}");
                    return false;
                }

                // Convert arguments to correct types
                var convertedArgs = new object[parsed.Arguments.Length];
                for (int i = 0; i < parsed.Arguments.Length; i++)
                {
                    if (i >= invoker.Parameters.Length)
                    {
                        Debug.LogError($"[TerminalValueRegistry] Too many arguments for action {parsed.Path}");
                        return false;
                    }

                    if (!TryConvertArgument(parsed.Arguments[i], invoker.Parameters[i].ParameterType, out convertedArgs[i]))
                    {
                        Debug.LogError($"[TerminalValueRegistry] Failed to convert argument '{parsed.Arguments[i]}' to {invoker.Parameters[i].ParameterType.Name}");
                        return false;
                    }
                }

                // Check parameter count
                if (convertedArgs.Length != invoker.Parameters.Length)
                {
                    Debug.LogError($"[TerminalValueRegistry] Action {parsed.Path} expects {invoker.Parameters.Length} arguments, got {convertedArgs.Length}");
                    return false;
                }

                // Invoke the action
                result = invoker.Invoke(convertedArgs);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerminalValueRegistry] Error executing action: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private class ActionCall
        {
            public string Path { get; set; }
            public string[] Arguments { get; set; }
        }

        private ActionCall ParseActionCall(string input)
        {
            // Parse: Players.player1.heal(50) or pause(true) or kill()
            var openParen = input.IndexOf('(');
            var closeParen = input.LastIndexOf(')');

            if (openParen == -1 || closeParen == -1 || closeParen <= openParen)
            {
                Debug.LogError($"[TerminalValueRegistry] Invalid action syntax: {input}");
                return null;
            }

            var path = input.Substring(0, openParen);
            var argsString = input.Substring(openParen + 1, closeParen - openParen - 1).Trim();

            string[] arguments;
            if (string.IsNullOrEmpty(argsString))
            {
                arguments = new string[0];
            }
            else
            {
                // Split by comma but not inside quotes or parentheses
                arguments = SplitArguments(argsString);
            }

            return new ActionCall
            {
                Path = path,
                Arguments = arguments
            };
        }

        private string[] SplitArguments(string argsString)
        {
            var args = new List<string>();
            var current = "";
            var depth = 0;
            var inQuotes = false;

            for (int i = 0; i < argsString.Length; i++)
            {
                var c = argsString[i];

                if (c == '"' && (i == 0 || argsString[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    current += c;
                }
                else if (!inQuotes && (c == '(' || c == '['))
                {
                    depth++;
                    current += c;
                }
                else if (!inQuotes && (c == ')' || c == ']'))
                {
                    depth--;
                    current += c;
                }
                else if (!inQuotes && depth == 0 && c == ',')
                {
                    args.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                args.Add(current.Trim());
            }

            return args.ToArray();
        }

        private bool TryConvertArgument(string rawArg, Type targetType, out object result)
        {
            result = null;

            try
            {
                // Remove quotes from strings
                if (rawArg.StartsWith("\"") && rawArg.EndsWith("\""))
                {
                    rawArg = rawArg.Substring(1, rawArg.Length - 2);
                }

                if (targetType == typeof(string))
                {
                    result = rawArg;
                    return true;
                }
                else if (targetType == typeof(int))
                {
                    result = int.Parse(rawArg);
                    return true;
                }
                else if (targetType == typeof(float))
                {
                    result = float.Parse(rawArg);
                    return true;
                }
                else if (targetType == typeof(double))
                {
                    result = double.Parse(rawArg);
                    return true;
                }
                else if (targetType == typeof(bool))
                {
                    result = bool.Parse(rawArg);
                    return true;
                }
                else if (targetType == typeof(Vector2))
                {
                    var parts = rawArg.Split(',');
                    if (parts.Length == 2)
                    {
                        result = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
                        return true;
                    }
                }
                else if (targetType == typeof(Vector3))
                {
                    var parts = rawArg.Split(',');
                    if (parts.Length == 3)
                    {
                        result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                        return true;
                    }
                }
                else if (targetType.IsEnum)
                {
                    result = Enum.Parse(targetType, rawArg, true);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
