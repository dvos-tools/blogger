using UnityEngine;
using com.DvosTools.blogger.Config;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace com.DvosTools.blogger.Service
{
    /// <summary>
    /// Service to abstract input handling between Legacy Input Manager and New Input System.
    /// </summary>
    public static class InputService
    {
        /// <summary>
        /// Check if the configured toggle key was pressed this frame.
        /// </summary>
        /// <param name="config">BLogger configuration containing input settings</param>
        /// <returns>True if the toggle key was pressed this frame</returns>
        public static bool IsToggleKeyPressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? Input.GetKeyDown(config.legacyToggleKey)
                : CheckNewInputToggle(config);
        }

        /// <summary>
        /// Check if the up arrow key was pressed this frame.
        /// </summary>
        /// <param name="config">BLogger configuration containing input system type</param>
        /// <returns>True if the up arrow key was pressed this frame</returns>
        public static bool IsUpArrowPressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? Input.GetKeyDown(KeyCode.UpArrow)
                : CheckNewInputUpArrow();
        }

        /// <summary>
        /// Check if the down arrow key was pressed this frame.
        /// </summary>
        /// <param name="config">BLogger configuration containing input system type</param>
        /// <returns>True if the down arrow key was pressed this frame</returns>
        public static bool IsDownArrowPressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? Input.GetKeyDown(KeyCode.DownArrow)
                : CheckNewInputDownArrow();
        }

#if ENABLE_INPUT_SYSTEM
        private static bool CheckNewInputToggle(BLoggerConfig config)
        {
            return Keyboard.current != null && Keyboard.current[config.newInputToggleKey].wasPressedThisFrame;
        }

        private static bool CheckNewInputUpArrow()
        {
            return Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame;
        }

        private static bool CheckNewInputDownArrow()
        {
            return Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame;
        }
#else
        private static bool CheckNewInputToggle(BLoggerConfig config)
        {
            Debug.LogWarning("[InputService] New Input System selected but not installed. Please install the Input System package or switch to Legacy Input Manager.");
            return false;
        }

        private static bool CheckNewInputUpArrow()
        {
            return false;
        }

        private static bool CheckNewInputDownArrow()
        {
            return false;
        }
#endif
    }
}
