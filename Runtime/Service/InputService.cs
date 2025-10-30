using UnityEngine;
using com.DvosTools.blogger.Config;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Service to abstract input handling between Legacy Input Manager and New Input System
    /// </summary>
    public static class InputService
    {
        /// <summary>
        /// Check if the configured toggle key was pressed this frame
        /// </summary>
        /// <param name="config">BLogger configuration containing input settings</param>
        /// <returns>True if the toggle key was pressed this frame</returns>
        public static bool IsToggleKeyPressed(BLoggerConfig config)
        {
            if (config.inputSystemType == InputSystemType.LegacyInputManager)
            {
                return CheckLegacyInput(config);
            }
            
            return CheckNewInput(config);
        }
        
        private static bool CheckLegacyInput(BLoggerConfig config)
        {
            return Input.GetKeyDown(config.legacyToggleKey);
        }
        
        #if ENABLE_INPUT_SYSTEM
        private static bool CheckNewInput(BLoggerConfig config)
        {
            return Keyboard.current != null && Keyboard.current[config.newInputToggleKey].wasPressedThisFrame;
        }
        #else
        private static bool CheckNewInput(BLoggerConfig config)
        {
            Debug.LogWarning("[InputService] New Input System selected but not installed. Please install the Input System package or switch to Legacy Input Manager.");
            return false;
        }
        #endif
    }
}
