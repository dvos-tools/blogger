using System;
using System.Collections.Generic;
using UnityEngine;
using com.DvosTools.blogger.Handlers;
using UnityEngine.Serialization;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace com.DvosTools.blogger.Config
{
    public enum InputSystemType
    {
        LegacyInputManager,
        NewInputSystem
    }
    
    /// <summary>
    /// ScriptableObject configuration for BLogger handlers
    /// </summary>
    [CreateAssetMenu(fileName = "BLoggerConfig", menuName = "BLogger/Configuration")]
    public class BLoggerConfig : ScriptableObject
    {
        [Header("File Handler Settings")]
        [Tooltip("Log file path (relative to persistent data path).\n\n" +
                 "Default: 'Logs/blogger.log'\n\n" +
                 "Platform-specific persistent data paths:\n" +
                 "• Windows: %userprofile%\\AppData\\LocalLow\\<companyname>\\<product-name>\\\n" +
                 "• macOS: ~/Library/Application Support/unity.<companyname>.<product-name>/Editor/\n" +
                 "Note: Directories will be created automatically if they don't exist.")]
        public string logFilePath = "Logs/blogger.log";
        
        [Header("OnScreen Handler Settings")]
        [Tooltip("Enable the on-screen terminal for debugging")]
        public bool enableOnScreenTerminal = true;
        
        [Tooltip("Maximum number of log entries to keep in memory (higher values use more memory)")]
        [Range(100, 5000)]
        public int maxOnScreenLogEntries = 1000;
        
        [Tooltip("Prefab for the on-screen terminal")]
        public GameObject onScreenTerminalPrefab;
        
        [Header("Input Settings")]
        [Tooltip("Which input system to use for toggle shortcut")]
        public InputSystemType inputSystemType = InputSystemType.LegacyInputManager;
        
        [Tooltip("Keyboard shortcut for Legacy Input Manager (KeyCode)")]
        public KeyCode legacyToggleKey = KeyCode.BackQuote;
        
        #if ENABLE_INPUT_SYSTEM
        [Tooltip("Keyboard shortcut for New Input System (Key)")]
        public Key newInputToggleKey = Key.Backquote;
        #endif
        
        [Tooltip("Enable Sentry")]
        public bool enableSentry = false;

        [Tooltip("Sentry DSN (e.g., http://key@localhost:9000/projectId)")] 
        public String sentryUrl = "";
        
        [Tooltip("Loki base URL (default: http://localhost:3100)")] 
        public string lokiUrl = "http://localhost:3100";
        
        private static BLoggerConfig _instance;
        
        /// <summary>
        /// Static instance of the BLoggerConfig
        /// </summary>
        public static BLoggerConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    LoadConfig();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Load the BLoggerConfig from Resources folder
        /// </summary>
        private static void LoadConfig()
        {
            // Try to find BLoggerConfig in the Resources folder
            _instance = Resources.Load<BLoggerConfig>("BLoggerConfig");
            
            if (_instance == null)
            {
                // Create a default config if none found
                _instance = CreateInstance<BLoggerConfig>();
            }
        }
        
        /// <summary>
        /// Create handlers based on this configuration
        /// </summary>
        /// <returns>List of initialized handlers</returns>
        public List<ILoggingHandler> CreateHandlers()
        {
            var handlers = new List<ILoggingHandler>();
            
            // Create FileHandler
            var fileHandler = new FileHandler(this);
            handlers.Add(fileHandler);
            
            // Create OnScreenHandler if enabled
            if (enableOnScreenTerminal && onScreenTerminalPrefab != null)
            {
                var terminalHandler = new OnScreenHandler(this);
                handlers.Add(terminalHandler);
            }

            if (enableSentry && sentryUrl.Length > 0 && lokiUrl.Length > 0)
            {
                var sentryHandler = new SentryHandler(this);
                handlers.Add(sentryHandler);
            }
            
            return handlers;
        }
        
        /// <summary>
        /// Set this as the active configuration
        /// </summary>
        public void SetAsActive()
        {
            _instance = this;
        }
    }
}