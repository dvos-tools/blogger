using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.DvosTools.blogger.Attributes;
using UnityEngine;

namespace com.DvosTools.blogger.Handlers.Terminal
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
                        _aggregateTypes.Add(type);

                    ScanStaticMembers(type);
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }

        Debug.Log(
            $"[TerminalValueRegistry] Found {_staticValues.Count} static values, {_staticActions.Count} static actions, and {_aggregateTypes.Count} aggregate types");
    }

    private void ScanStaticMembers(Type type)
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;

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

    /// <summary>
    /// Register an instance (MonoBehaviour or regular object) with the registry
    /// </summary>
    public void RegisterInstance(object instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("[TerminalValueRegistry] Cannot register null instance");
            return;
        }

        var type = instance.GetType();

        if (!_aggregateTypes.Contains(type))
        {
            // Check if this type has the aggregate attribute even if not scanned
            var aggregateAttr = type.GetCustomAttribute<BLoggerAggregateAttribute>();
            if (aggregateAttr != null)
                _aggregateTypes.Add(type);
            else
            {
                Debug.LogWarning($"[TerminalValueRegistry] Type {type.Name} does not have [BLoggerAggregate] attribute");
                return;
            }
        }

        var aggregateAttribute = type.GetCustomAttribute<BLoggerAggregateAttribute>();
        if (aggregateAttribute == null) return;

        var aggregateName = aggregateAttribute.AggregateName;

        // Find the field or property marked with [BLoggerAggregateId]
        var instanceKey = TerminalHelper.GetInstanceKey(instance);
        if (string.IsNullOrEmpty(instanceKey))
        {
            Debug.LogWarning($"[TerminalValueRegistry] Instance of type {type.Name} has no [BLoggerAggregateId] field/property defined");
            return;
        }

        var fullKey = (aggregateName, instanceKey);

        if (_registeredInstances.TryGetValue(fullKey, out var existing))
        {
            if (existing.IsAlive && existing.Target != null)
            {
                // If it's the exact same instance, skip re-registration
                if (ReferenceEquals(existing.Target, instance))
                    return;
                
                Debug.LogWarning($"[TerminalValueRegistry] Instance '{aggregateName}.{instanceKey}' already registered. Overwriting.");
            }
        }

        _registeredInstances[fullKey] = new WeakReference(instance);

        ScanInstanceMembers(instance, aggregateName, instanceKey);

        Debug.Log(
            $"[TerminalValueRegistry] Registered instance '{aggregateName}.{instanceKey}' of type {type.Name}");
    }

   

    private void ScanInstanceMembers(object instance, string aggregateName, string instanceKey)
    {
        var type = instance.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

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

    /// <summary>
    /// Unregister an instance (MonoBehaviour or regular object) from the terminal registry
    /// </summary>
    public void UnregisterInstance(object instance)
    {
        if (instance == null) return;

        var type = instance.GetType();
        var aggregateAttr = type.GetCustomAttribute<BLoggerAggregateAttribute>();
        if (aggregateAttr == null) return;

        var aggregateName = aggregateAttr.AggregateName;
        var instanceKey = TerminalHelper.GetInstanceKey(instance);
        if (string.IsNullOrEmpty(instanceKey)) return;

        var fullKey = (aggregateName, instanceKey);
        _registeredInstances.Remove(fullKey);

        var valuesToRemove = _instanceValues.Keys
            .Where(k => k.aggregateName == aggregateName && k.instanceKey == instanceKey)
            .ToList();
        foreach (var key in valuesToRemove) _instanceValues.Remove(key);

        var actionsToRemove = _instanceActions.Keys
            .Where(k => k.aggregateName == aggregateName && k.instanceKey == instanceKey)
            .ToList();
        foreach (var key in actionsToRemove) _instanceActions.Remove(key);

        Debug.Log($"[TerminalValueRegistry] Unregistered instance '{aggregateName}.{instanceKey}'");
    }

    public bool TryGetValue(string token, out object value)
    {
        value = null;

        var parts = token.Split('.');

        switch (parts.Length)
        {
            // Static value: @fps
            case 1:
            {
                if (!_staticValues.TryGetValue(token, out var accessor))
                    return false;
            
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

                break;
            }
            // Instance value: @AggregateName.instanceKey.valueName
            case 3:
            {
                var aggregateName = parts[0];
                var instanceKey = parts[1];
                var valueName = parts[2];

                var key = (aggregateName, instanceKey, valueName);

                if (!_instanceValues.TryGetValue(key, out var accessor))
                    return false;

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

                break;
            }
            default:
                return false;
        }
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
            var parsed = InputParserService.ParseActionCall(actionToken);
            if (parsed == null)
                return false;

            ActionInvoker invoker;

            // Check if it's a static action or instance action
            var pathParts = parsed.Path.Split('.');

            if (pathParts.Length == 1)
            {
                // Static action: !pause(true)
                if (!_staticActions.TryGetValue(pathParts[0], out invoker))
                    return false;
            }
            else if (pathParts.Length == 3)
            {
                // Instance action: !Players.player1.heal(50)
                var aggregateName = pathParts[0];
                var instanceKey = pathParts[1];
                var actionName = pathParts[2];

                var key = (aggregateName, instanceKey, actionName);
                if (!_instanceActions.TryGetValue(key, out invoker))
                    return false;
            }
            else
            {
                return false;
            }

            // Convert arguments to correct types
            var convertedArgs = new object[parsed.Arguments.Length];
            for (int i = 0; i < parsed.Arguments.Length; i++)
            {
                if (i >= invoker.Parameters.Length)
                    return false;

                if (!TryConvertArgument(parsed.Arguments[i], invoker.Parameters[i].ParameterType,
                        out convertedArgs[i]))
                    return false;
            }

            // Check parameter count
            if (convertedArgs.Length != invoker.Parameters.Length)
                return false;

            // Invoke the action
            result = invoker.Invoke(convertedArgs);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool TryConvertArgument(string rawArg, Type targetType, out object result)
    {
        result = null;

        try
        {
            rawArg = InputParserService.StripQuotes(rawArg);

            // Handle enum types separately since they can't be in the switch
            if (targetType.IsEnum)
            {
                result = Enum.Parse(targetType, rawArg, ignoreCase: true);
                return true;
            }

            // Use Type.GetTypeCode for built-in types
            result = Type.GetTypeCode(targetType) switch
            {
                TypeCode.String => rawArg,
                TypeCode.Int32 => int.Parse(rawArg),
                TypeCode.Single => float.Parse(rawArg),
                TypeCode.Double => double.Parse(rawArg),
                TypeCode.Boolean => bool.Parse(rawArg),
                _ => InputParserService.TryParseUnityType(rawArg, targetType)
            };

            return result != null;
        }
        catch
        {
            return false;
        }
    }
    }
}