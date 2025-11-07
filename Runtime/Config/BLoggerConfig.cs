using System.Collections.Generic;
using UnityEngine;
using com.DvosTools.blogger.Handlers;
using com.DvosTools.blogger.Handlers.Terminal;

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
        [Header("Terminal")]
        [Space]
        [Tooltip("Enable the on-screen terminal for debugging")]
        public bool enableOnScreenTerminal = true;
        
        [Tooltip("Maximum number of log entries to keep in memory (higher values use more memory)")]
        [Range(100, 5000)]
        public int maxOnScreenLogEntries = 1000;
        
        [Tooltip("Prefab for the on-screen terminal")]
        public GameObject onScreenTerminalPrefab;
        
        [Tooltip("Which input system to use for toggle shortcut")]
        public InputSystemType inputSystemType = InputSystemType.LegacyInputManager;
        
        [Tooltip("Keyboard shortcut for Legacy Input Manager (KeyCode)")]
        public KeyCode legacyToggleKey = KeyCode.BackQuote;
        
        #if ENABLE_INPUT_SYSTEM
        [Tooltip("Keyboard shortcut for New Input System (Key)")]
        public Key newInputToggleKey = Key.Backquote;
        #endif
        
        [Tooltip("Use Command key (Mac) or Control key (Windows/Linux) for font size shortcuts")]
        public bool useCommandKeyForFontSize = true;
        
        [Tooltip("Initial font size for the terminal")]
        [Range(8, 72)]
        public int terminalFontSize = 14;
        
        [Tooltip("Minimum font size for the terminal")]
        [Range(8, 36)]
        public int minTerminalFontSize = 8;
        
        [Tooltip("Maximum font size for the terminal")]
        [Range(24, 72)]
        public int maxTerminalFontSize = 48;
        
        [Header("File")]
        [Space]
        [Tooltip("Log file path (relative to persistent data path).\n\n" +
                 "Default: 'Logs/blogger.log'\n\n" +
                 "Platform-specific persistent data paths:\n" +
                 "• Windows: %userprofile%\\AppData\\LocalLow\\<companyname>\\<product-name>\\\n" +
                 "• macOS: ~/Library/Application Support/unity.<companyname>.<product-name>/Editor/\n" +
                 "Note: Directories will be created automatically if they don't exist.")]
        public string logFilePath = "Logs/blogger.log";
        
        [Header("WebLogging")]
        [Space]
        [Tooltip("Loki base URL (default: http://localhost:3100)")] 
        public string lokiUrl = "http://localhost:3100";
        
        [Tooltip("Enable sampling to reduce log volume in production\n\n" +
                 "When enabled, only a percentage of logs will be sent to handlers.\n" +
                 "Errors and Exceptions are always logged (100% sampling).")]
        public bool enableSampling = false;
        
        [Tooltip("Sample rate for Info/Log messages (0.0 = none, 1.0 = all)\n\n" +
                 "Example: 0.1 = log 10% of info messages\n" +
                 "Recommended for production: 0.1 - 0.5")]
        [Range(0f, 1f)]
        public float infoSampleRate = 1.0f;
        
        [Tooltip("Sample rate for Warning messages (0.0 = none, 1.0 = all)\n\n" +
                 "Example: 0.5 = log 50% of warnings\n" +
                 "Recommended for production: 0.5 - 1.0")]
        [Range(0f, 1f)]
        public float warningSampleRate = 1.0f;
        
        [Tooltip("Sample rate for Errors (0.0 = none, 1.0 = all)\n\n" +
                 "Usually keep at 1.0 to capture all errors!\n" +
                 "Only reduce if experiencing extreme log volume.")]
        [Range(0f, 1f)]
        public float errorSampleRate = 1.0f;
        
        [Tooltip("Always log Exceptions (ignores sampling)\n\n" +
                 "Recommended: Keep enabled! Exceptions are critical.")]
        public bool alwaysLogExceptions = true;
        
        [Tooltip("Max logs per second (rate limiting)\n\n" +
                 "Prevents log storms from overwhelming services.\n" +
                 "0 = unlimited\n" +
                 "Recommended for production: 50-200")]
        [Range(0, 1000)]
        public int maxLogsPerSecond = 0;
        
        private static BLoggerConfig _instance;
        
        /// <summary>
        /// Static instance of the BLoggerConfig
        /// </summary>
        public static BLoggerConfig Instance
        {
            get
            {
                if (!_instance) LoadConfig();
                return _instance;
            }
        }
        
        /// <summary>
        /// Load the BLoggerConfig from Resources folder.
        /// Loading priority:
        /// 1. User's project: Assets/Resources/BLoggerConfig.asset (editable)
        /// 2. Package template: Packages/.../BLoggerConfigTemplate.asset (read-only backup)
        /// 3. Runtime defaults: Created on-the-fly if nothing found
        /// </summary>
        private static void LoadConfig()
        {
            #if UNITY_EDITOR
            // PRIORITY 1: Try to load from user's project first (Assets/Resources/)
            _instance = Resources.Load<BLoggerConfig>("BLoggerConfig");
            
            if (_instance)
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(_instance);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/"))
                {
                    Debug.Log($"[BLogger] Loaded config from project: {assetPath}");
                    return;
                }
            }
            
            // PRIORITY 2: Fall back to package template if no user config exists
            _instance = Resources.Load<BLoggerConfig>("BLoggerConfigTemplate");
            
            if (_instance)
            {
                var templatePath = UnityEditor.AssetDatabase.GetAssetPath(_instance);
                Debug.LogWarning($"[BLogger] No config found in Assets/Resources/. Using package template: {templatePath}\n" +
                                "This config is READ-ONLY. Create your own via: Tools > BLogger > Create Config");
                return;
            }
            
            // If we get here, package installation is broken
            Debug.LogError("[BLogger] CRITICAL: Cannot find BLoggerConfigTemplate.asset in package! " +
                          "Package installation may be corrupted. Please reinstall the package.");
            #else
            // In builds, try to load user's config
            _instance = Resources.Load<BLoggerConfig>("BLoggerConfig");
            
            // Fallback to template in builds
            if (_instance == null)
            {
                _instance = Resources.Load<BLoggerConfig>("BLoggerConfigTemplate");
            }
            
            if (_instance == null)
            {
                Debug.LogError("[BLogger] CRITICAL: Cannot find BLoggerConfig! " +
                              "Make sure BLoggerConfig.asset or BLoggerConfigTemplate.asset exists in a Resources folder.");
            }
            #endif
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
            
            // Create TerminalHandler if enabled
            if (enableOnScreenTerminal && onScreenTerminalPrefab != null)
            {
                var terminalHandler = new TerminalHandler(this);
                handlers.Add(terminalHandler);
            }

            // Create LokiHandler if configured
            if (lokiUrl.Length <= 0) return handlers;
            var lokiHandler = new LokiHandler(this);
            handlers.Add(lokiHandler);

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