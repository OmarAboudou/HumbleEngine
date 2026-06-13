namespace HumbleEngine;

/// <summary>
/// Logical keyboard key — what shortcuts and navigation consume (arrows,
/// Escape, Ctrl+S…). <b>Not text</b>: composed characters (layout, dead keys)
/// travel separately as <see cref="TextInput"/> — a text field never reads
/// keys, a shortcut never reads text.
/// </summary>
public enum Key
{
    Unknown = 0,

    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    Digit0, Digit1, Digit2, Digit3, Digit4,
    Digit5, Digit6, Digit7, Digit8, Digit9,

    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    Space, Enter, Escape, Tab, Backspace, Delete, Insert,
    Left, Right, Up, Down,
    Home, End, PageUp, PageDown,
    LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt, Super,
}

/// <summary>Modifier keys held while an event fired.</summary>
[Flags]
public enum KeyModifiers
{
    None  = 0,
    Shift = 1 << 0,
    Ctrl  = 1 << 1,
    Alt   = 1 << 2,
    Super = 1 << 3,
}

/// <summary>
/// Keysym → <see cref="Key"/> translation shared by the Linux windowing
/// backends — X11 keysyms and xkbcommon keysyms are the same vocabulary
/// (common XKB heritage), so one table serves both. Plumbing, not application
/// API: applications consume <see cref="Key"/>, never keysyms.
/// </summary>
public static class KeysymTranslation
{
    /// <summary>Maps an XKB keysym to the logical key, <see cref="Key.Unknown"/> when unmapped.</summary>
    public static Key ToKey(uint keysym) => keysym switch
    {
        >= 0x0061 and <= 0x007a => Key.A + (int)(keysym - 0x0061),      // a..z
        >= 0x0041 and <= 0x005a => Key.A + (int)(keysym - 0x0041),      // A..Z (shifted)
        >= 0x0030 and <= 0x0039 => Key.Digit0 + (int)(keysym - 0x0030), // 0..9
        >= 0xffbe and <= 0xffc9 => Key.F1 + (int)(keysym - 0xffbe),     // F1..F12
        0x0020          => Key.Space,
        0xff0d or 0xff8d => Key.Enter, // Return, keypad Enter
        0xff1b => Key.Escape,
        0xff09 => Key.Tab,
        0xff08 => Key.Backspace,
        0xffff => Key.Delete,
        0xff63 => Key.Insert,
        0xff51 => Key.Left,
        0xff52 => Key.Up,
        0xff53 => Key.Right,
        0xff54 => Key.Down,
        0xff50 => Key.Home,
        0xff57 => Key.End,
        0xff55 => Key.PageUp,
        0xff56 => Key.PageDown,
        0xffe1 => Key.LeftShift,
        0xffe2 => Key.RightShift,
        0xffe3 => Key.LeftCtrl,
        0xffe4 => Key.RightCtrl,
        0xffe9 => Key.LeftAlt,
        0xffea => Key.RightAlt,
        0xffeb or 0xffec => Key.Super,
        _ => Key.Unknown,
    };
}
