using UnityEngine;
using System.Collections.Generic;

public static class KeyCodeExtensions
{
    private static readonly Dictionary<KeyCode, string> keyCodeDisplayNames = new Dictionary<KeyCode, string>()
    {
        { KeyCode.Space, "Space Bar" },
        { KeyCode.Return, "Enter" },
        { KeyCode.KeypadEnter, "Numpad Enter" },
        { KeyCode.LeftShift, "Left Shift" },
        { KeyCode.RightShift, "Right Shift" },
        { KeyCode.LeftControl, "Left Ctrl" },
        { KeyCode.RightControl, "Right Ctrl" },
        { KeyCode.LeftAlt, "Left Alt" },
        { KeyCode.RightAlt, "Right Alt" },
        { KeyCode.Escape, "Esc" },
        { KeyCode.BackQuote, "`" },
        { KeyCode.Minus, "-" },
        { KeyCode.Equals, "=" },
        { KeyCode.Backspace, "Backspace" },
        { KeyCode.Tab, "Tab" },
        { KeyCode.CapsLock, "Caps Lock" },
        { KeyCode.Alpha0, "0" },
        { KeyCode.Alpha1, "1" },
        { KeyCode.Alpha2, "2" },
        { KeyCode.Alpha3, "3" },
        { KeyCode.Alpha4, "4" },
        { KeyCode.Alpha5, "5" },
        { KeyCode.Alpha6, "6" },
        { KeyCode.Alpha7, "7" },
        { KeyCode.Alpha8, "8" },
        { KeyCode.Alpha9, "9" },
    };

    public static string ToReadableString(this KeyCode keyCode)
    {
        if (keyCodeDisplayNames.TryGetValue(keyCode, out string displayName))
        {
            return displayName;
        }
        return keyCode.ToString();
    }
}