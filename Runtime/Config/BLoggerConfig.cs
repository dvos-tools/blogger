using System.Collections.Generic;
using UnityEngine;
using com.DvosTools.blogger.Handlers;

namespace com.DvosTools.blogger.Config
{
    /// <summary>
    /// Enum defining available logging handler types
    /// </summary>
    public enum LoggingHandlerType
    {
        File
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