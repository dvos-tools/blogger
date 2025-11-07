#nullable enable
using com.DvosTools.blogger.Handlers.Terminal;
using UnityEngine;
using com.DvosTools.blogger.Service;

namespace com.DvosTools.blogger 
{
    /// <summary>
    /// Static API for BLogger - provides simple logging methods that delegate to <see cref="BLoggerService"/>.
    /// <br/>
    /// Matches <see cref="UnityEngine.Debug.Log(object)"/> signature for easy migration from Unity's built-in logging.
    /// </summary>
    public static class BLogger
    {
        /// <summary>
        /// Log an info message.
        /// <br/>
        /// Drop-in replacement for <see cref="UnityEngine.Debug.Log(object)"/>.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        /// <example>
        /// <code>
        /// BLogger.Log("Player spawned");
        /// BLogger.Log("Health: " + health, includeStackTrace: true);
        /// </code>
        /// </example>
        public static void Log(object? message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Log);
            
            // Also log to Unity's Debug.Log for console visibility
            BLoggerService.IsBLoggerDebugLog = true;
            try
            {
                Debug.Log(message);
            }
            finally
            {
                BLoggerService.IsBLoggerDebugLog = false;
            }
        }

        /// <summary>
        /// Log a warning message.
        /// <br/>
        /// Drop-in replacement for <see cref="UnityEngine.Debug.LogWarning(object)"/>.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        /// <example>
        /// <code>
        /// BLogger.Warn("Low health detected");
        /// </code>
        /// </example>
        public static void Warn(object? message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Warning);
            
            // Also log to Unity's Debug.LogWarning for console visibility
            BLoggerService.IsBLoggerDebugLog = true;
            try
            {
                Debug.LogWarning(message);
            }
            finally
            {
                BLoggerService.IsBLoggerDebugLog = false;
            }
        }

        /// <summary>
        /// Log an error message.
        /// <br/>
        /// Drop-in replacement for <see cref="UnityEngine.Debug.LogError(object)"/>.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="includeStackTrace">Whether to automatically capture stack trace (default: false for performance)</param>
        /// <example>
        /// <code>
        /// BLogger.Error("Failed to load save file");
        /// </code>
        /// </example>
        public static void Error(object? message, bool includeStackTrace = false)
        {
            var stackTrace = includeStackTrace ? System.Environment.StackTrace : "";
            BLoggerService.Instance.HandleLog(message?.ToString() ?? "", stackTrace, LogType.Error);
            
            // Also log to Unity's Debug.LogError for console visibility
            BLoggerService.IsBLoggerDebugLog = true;
            try
            {
                Debug.LogError(message);
            }
            finally
            {
                BLoggerService.IsBLoggerDebugLog = false;
            }
        }

        /// <summary>
        /// Register an instance to be accessible via the terminal.
        /// <br/>
        /// Use this for objects marked with <see cref="Attributes.BLoggerAggregateAttribute"/>.
        /// </summary>
        /// <param name="instance">The instance to register (can be MonoBehaviour or regular object)</param>
        /// <example>
        /// <code>
        /// [BLoggerAggregate("Player")]
        /// public class PlayerController : MonoBehaviour
        /// {
        ///     void Start()
        ///     {
        ///         BLogger.RegisterValue(this);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void RegisterValue(object instance)
        {
            TerminalValueRegistry.Instance.RegisterInstance(instance);
        }

        /// <summary>
        /// Unregister an instance from the terminal.
        /// <br/>
        /// Call this when the instance is destroyed or no longer needed.
        /// </summary>
        /// <param name="instance">The instance to unregister</param>
        /// <example>
        /// <code>
        /// void OnDestroy()
        /// {
        ///     BLogger.UnregisterValue(this);
        /// }
        /// </code>
        /// </example>
        public static void UnregisterValue(object instance)
        {
            TerminalValueRegistry.Instance.UnregisterInstance(instance);
        }

        /// <summary>
        /// Auto-register a <see cref="UnityEngine.MonoBehaviour"/> for use with the terminal system.
        /// <br/>
        /// Call this in your <see cref="UnityEngine.MonoBehaviour.Awake"/> method for automatic registration.
        /// <br/>
        /// Works with any base class (<see cref="UnityEngine.MonoBehaviour"/>, <c>NetworkBehaviour</c>, etc.)
        /// </summary>
        /// <param name="component">The component to register</param>
        /// <example>
        /// <code>
        /// [BLoggerAggregate("Player")]
        /// public class PlayerController : NetworkBehaviour
        /// {
        ///     void Awake()
        ///     {
        ///         BLogger.AutoRegister(this);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void AutoRegister(MonoBehaviour component)
        {
            if (component == null) return;
            TerminalValueRegistry.Instance.RegisterInstance(component);
        }

        /// <summary>
        /// Auto-unregister a <see cref="UnityEngine.MonoBehaviour"/> from the terminal system.
        /// <br/>
        /// Call this in your <see cref="UnityEngine.MonoBehaviour.OnDestroy"/> method.
        /// </summary>
        /// <param name="component">The component to unregister</param>
        /// <example>
        /// <code>
        /// void OnDestroy()
        /// {
        ///     BLogger.AutoUnregister(this);
        /// }
        /// </code>
        /// </example>
        public static void AutoUnregister(MonoBehaviour component)
        {
            if (component == null) return;
            TerminalValueRegistry.Instance.UnregisterInstance(component);
        }
    }
}

