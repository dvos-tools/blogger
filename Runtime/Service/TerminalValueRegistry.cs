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
        private readonly HashSet<Type> _aggregateTypes = new();
        private readonly Dictionary<(string aggregateName, string instanceKey), WeakReference> _registeredInstances = new();

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

            Debug.Log($"[TerminalValueRegistry] Found {_staticValues.Count} static terminal values and {_aggregateTypes.Count} aggregate types");
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
                var attr = method.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null && method.GetParameters().Length == 0)
                {
                    _staticValues[attr.Name] = () => method.Invoke(null, null);
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
                var attr = method.GetCustomAttribute<BLoggerValueAttribute>();
                if (attr != null && method.GetParameters().Length == 0)
                {
                    var key = (aggregateName, instanceKey, attr.Name);
                    _instanceValues[key] = () => method.Invoke(instance, null);
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

            var keysToRemove = _instanceValues.Keys
                .Where(k => k.aggregateName == aggregateName && k.instanceKey == instanceKey)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _instanceValues.Remove(key);
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
    }
}
