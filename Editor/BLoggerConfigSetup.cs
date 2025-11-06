using UnityEngine;
using UnityEditor;
using System.IO;
using com.DvosTools.blogger.Config;

namespace com.DvosTools.blogger.Editor
{
    /// <summary>
    /// Automatically creates BLoggerConfig in the user's project on first import.
    /// Uses the package template as a starting point.
    /// </summary>
    [InitializeOnLoad]
    public static class BLoggerConfigSetup
    {
        private const string ConfigPath = "Assets/Resources/BLoggerConfig.asset";
        private const string ResourcesFolder = "Assets/Resources";
        private const string PackageTemplatePath = "Packages/com.dvos-tools.blogger/Runtime/Resources/BLoggerConfigTemplate.asset";
        
        static BLoggerConfigSetup()
        {
            // Use EditorApplication.delayCall to ensure Unity is fully initialized
            EditorApplication.delayCall += EnsureConfigExists;
        }
        
        [MenuItem("Tools/BLogger/Create Config (if missing)", priority = 1)]
        public static void EnsureConfigExists()
        {
            // Check if config already exists in user's project
            if (File.Exists(ConfigPath))
            {
                Debug.Log($"[BLogger] Config already exists at {ConfigPath}");
                return;
            }
            
            // Ensure Resources folder exists
            if (!Directory.Exists(ResourcesFolder))
            {
                Directory.CreateDirectory(ResourcesFolder);
                AssetDatabase.Refresh();
            }
            
            // Try to copy from package template
            var packageTemplate = AssetDatabase.LoadAssetAtPath<BLoggerConfig>(PackageTemplatePath);
            if (packageTemplate)
            {
                // Copy the package template to user's project
                AssetDatabase.CopyAsset(PackageTemplatePath, ConfigPath);
                
                // Update the name to remove "Template" suffix
                var newConfig = AssetDatabase.LoadAssetAtPath<BLoggerConfig>(ConfigPath);
                if (newConfig)
                {
                    newConfig.name = "BLoggerConfig";
                    EditorUtility.SetDirty(newConfig);
                }
                
                Debug.Log($"[BLogger] Created config at {ConfigPath} (copied from package template)");
            }
            else
            {
                // Fallback: Create a new default config
                var config = ScriptableObject.CreateInstance<BLoggerConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[BLogger] Created new config at {ConfigPath} (using defaults)");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Ping the asset so user can see it
            var createdConfig = AssetDatabase.LoadAssetAtPath<BLoggerConfig>(ConfigPath);
            if (createdConfig)
                EditorGUIUtility.PingObject(createdConfig);
        }
        
        [MenuItem("Tools/BLogger/Reset Config to Defaults", priority = 2)]
        public static void ResetConfigToDefaults()
        {
            if (!EditorUtility.DisplayDialog(
                "Reset BLogger Config",
                "This will delete your current config and create a fresh one with default settings. Continue?",
                "Yes, Reset",
                "Cancel"))
            {
                return;
            }
            
            // Delete existing config
            if (File.Exists(ConfigPath))
            {
                AssetDatabase.DeleteAsset(ConfigPath);
            }
            
            // Create new one
            EnsureConfigExists();
        }
        
        [MenuItem("Tools/BLogger/Open Config", priority = 3)]
        public static void OpenConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<BLoggerConfig>(ConfigPath);
            if (config)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                if (EditorUtility.DisplayDialog(
                    "Config Not Found",
                    $"No config found at {ConfigPath}. Create one now?",
                    "Create",
                    "Cancel"))
                {
                    EnsureConfigExists();
                }
            }
        }
    }
}
