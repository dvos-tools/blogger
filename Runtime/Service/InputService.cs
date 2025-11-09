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

        /// <summary>
        /// Check if the font size increase shortcut was pressed this frame (CMD/CTRL + Plus).
        /// </summary>
        /// <param name="config">BLogger configuration containing input settings</param>
        /// <returns>True if the font size increase shortcut was pressed this frame</returns>
        public static bool IsFontSizeIncreasePressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? CheckLegacyFontSizeIncrease(config)
                : CheckNewInputFontSizeIncrease(config);
        }

        /// <summary>
        /// Check if the font size decrease shortcut was pressed this frame (CMD/CTRL + Minus).
        /// </summary>
        /// <param name="config">BLogger configuration containing input settings</param>
        /// <returns>True if the font size decrease shortcut was pressed this frame</returns>
        public static bool IsFontSizeDecreasePressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? CheckLegacyFontSizeDecrease(config)
                : CheckNewInputFontSizeDecrease(config);
        }

        /// <summary>
        /// Check if the Tab key was pressed this frame.
        /// </summary>
        /// <param name="config">BLogger configuration containing input system type</param>
        /// <returns>True if the Tab key was pressed this frame</returns>
        public static bool IsTabPressed(BLoggerConfig config)
        {
            return config.inputSystemType == InputSystemType.LegacyInputManager
                ? Input.GetKeyDown(KeyCode.Tab)
                : CheckNewInputTab();
        }

        private static bool CheckLegacyFontSizeIncrease(BLoggerConfig config)
        {
            bool modifierPressed = config.useCommandKeyForFontSize
                ? Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
                : Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            return modifierPressed && (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus));
        }

        private static bool CheckLegacyFontSizeDecrease(BLoggerConfig config)
        {
            bool modifierPressed = config.useCommandKeyForFontSize
                ? Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
                : Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            return modifierPressed && (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus));
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

        private static bool CheckNewInputTab()
        {
            return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        }

        private static bool CheckNewInputFontSizeIncrease(BLoggerConfig config)
        {
            if (Keyboard.current == null) return false;

            bool modifierPressed = config.useCommandKeyForFontSize
                ? Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed
                : Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;

            return modifierPressed && (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame);
        }

        private static bool CheckNewInputFontSizeDecrease(BLoggerConfig config)
        {
            if (Keyboard.current == null) return false;

            bool modifierPressed = config.useCommandKeyForFontSize
                ? Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed
                : Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;

            return modifierPressed && (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame);
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

        private static bool CheckNewInputTab()
        {
            return false;
        }

        private static bool CheckNewInputFontSizeIncrease(BLoggerConfig config)
        {
            Debug.LogWarning("[InputService] New Input System selected but not installed. Please install the Input System package or switch to Legacy Input Manager.");
            return false;
        }

        private static bool CheckNewInputFontSizeDecrease(BLoggerConfig config)
        {
            Debug.LogWarning("[InputService] New Input System selected but not installed. Please install the Input System package or switch to Legacy Input Manager.");
            return false;
        }
#endif
    }
}
