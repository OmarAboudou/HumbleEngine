using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkKb  = Silk.NET.Input.IKeyboard;
using SilkKey = Silk.NET.Input.Key;

namespace HumbleEngine.Silk;

public class SilkKeyboard : IKeyboard
{
    private readonly SilkKb _keyboard;

    private readonly Signal<Key, int> _onKeyDown = new();
    private readonly Signal<Key, int> _onKeyUp   = new();
    private readonly Signal<char>     _onKeyChar = new();

    public SilkKeyboard(SilkKb keyboard)
    {
        _keyboard = keyboard;
        _keyboard.KeyDown += (_, k, sc) => _onKeyDown.Emit(FromSilk(k), sc);
        _keyboard.KeyUp   += (_, k, sc) => _onKeyUp.Emit(FromSilk(k), sc);
        _keyboard.KeyChar += (_, c)     => _onKeyChar.Emit(c);
    }

    public IReadOnlyList<Key> SupportedKeys => _keyboard.SupportedKeys.Select(FromSilk).ToList();
    public string             ClipboardText { get => _keyboard.ClipboardText; set => _keyboard.ClipboardText = value; }

    public bool IsKeyPressed(Key key)        => _keyboard.IsKeyPressed(ToSilk(key));
    public bool IsScancodePressed(int sc)    => _keyboard.IsScancodePressed(sc);

    public void BeginInput() => _keyboard.BeginInput();
    public void EndInput()   => _keyboard.EndInput();

    public IReadOnlySignal<Key, int> OnKeyDown => _onKeyDown.AsReadOnly();
    public IReadOnlySignal<Key, int> OnKeyUp   => _onKeyUp.AsReadOnly();
    public IReadOnlySignal<char>     OnKeyChar => _onKeyChar.AsReadOnly();

    private static Key FromSilk(SilkKey k) => k switch
    {
        SilkKey.Space        => Key.Space,        SilkKey.Apostrophe   => Key.Apostrophe,
        SilkKey.Comma        => Key.Comma,         SilkKey.Minus        => Key.Minus,
        SilkKey.Period       => Key.Period,        SilkKey.Slash        => Key.Slash,
        SilkKey.Number0      => Key.Number0,       SilkKey.Number1      => Key.Number1,
        SilkKey.Number2      => Key.Number2,       SilkKey.Number3      => Key.Number3,
        SilkKey.Number4      => Key.Number4,       SilkKey.Number5      => Key.Number5,
        SilkKey.Number6      => Key.Number6,       SilkKey.Number7      => Key.Number7,
        SilkKey.Number8      => Key.Number8,       SilkKey.Number9      => Key.Number9,
        SilkKey.Semicolon    => Key.Semicolon,     SilkKey.Equal        => Key.Equal,
        SilkKey.A            => Key.A,             SilkKey.B            => Key.B,
        SilkKey.C            => Key.C,             SilkKey.D            => Key.D,
        SilkKey.E            => Key.E,             SilkKey.F            => Key.F,
        SilkKey.G            => Key.G,             SilkKey.H            => Key.H,
        SilkKey.I            => Key.I,             SilkKey.J            => Key.J,
        SilkKey.K            => Key.K,             SilkKey.L            => Key.L,
        SilkKey.M            => Key.M,             SilkKey.N            => Key.N,
        SilkKey.O            => Key.O,             SilkKey.P            => Key.P,
        SilkKey.Q            => Key.Q,             SilkKey.R            => Key.R,
        SilkKey.S            => Key.S,             SilkKey.T            => Key.T,
        SilkKey.U            => Key.U,             SilkKey.V            => Key.V,
        SilkKey.W            => Key.W,             SilkKey.X            => Key.X,
        SilkKey.Y            => Key.Y,             SilkKey.Z            => Key.Z,
        SilkKey.LeftBracket  => Key.LeftBracket,   SilkKey.BackSlash    => Key.BackSlash,
        SilkKey.RightBracket => Key.RightBracket,  SilkKey.GraveAccent  => Key.GraveAccent,
        SilkKey.Escape       => Key.Escape,        SilkKey.Enter        => Key.Enter,
        SilkKey.Tab          => Key.Tab,           SilkKey.Backspace    => Key.Backspace,
        SilkKey.Insert       => Key.Insert,        SilkKey.Delete       => Key.Delete,
        SilkKey.Right        => Key.Right,         SilkKey.Left         => Key.Left,
        SilkKey.Down         => Key.Down,          SilkKey.Up           => Key.Up,
        SilkKey.PageUp       => Key.PageUp,        SilkKey.PageDown     => Key.PageDown,
        SilkKey.Home         => Key.Home,          SilkKey.End          => Key.End,
        SilkKey.CapsLock     => Key.CapsLock,      SilkKey.ScrollLock   => Key.ScrollLock,
        SilkKey.NumLock      => Key.NumLock,       SilkKey.PrintScreen  => Key.PrintScreen,
        SilkKey.Pause        => Key.Pause,
        SilkKey.F1  => Key.F1,  SilkKey.F2  => Key.F2,  SilkKey.F3  => Key.F3,
        SilkKey.F4  => Key.F4,  SilkKey.F5  => Key.F5,  SilkKey.F6  => Key.F6,
        SilkKey.F7  => Key.F7,  SilkKey.F8  => Key.F8,  SilkKey.F9  => Key.F9,
        SilkKey.F10 => Key.F10, SilkKey.F11 => Key.F11, SilkKey.F12 => Key.F12,
        SilkKey.F13 => Key.F13, SilkKey.F14 => Key.F14, SilkKey.F15 => Key.F15,
        SilkKey.F16 => Key.F16, SilkKey.F17 => Key.F17, SilkKey.F18 => Key.F18,
        SilkKey.F19 => Key.F19, SilkKey.F20 => Key.F20, SilkKey.F21 => Key.F21,
        SilkKey.F22 => Key.F22, SilkKey.F23 => Key.F23, SilkKey.F24 => Key.F24,
        SilkKey.F25 => Key.F25,
        SilkKey.Keypad0        => Key.Keypad0,        SilkKey.Keypad1        => Key.Keypad1,
        SilkKey.Keypad2        => Key.Keypad2,        SilkKey.Keypad3        => Key.Keypad3,
        SilkKey.Keypad4        => Key.Keypad4,        SilkKey.Keypad5        => Key.Keypad5,
        SilkKey.Keypad6        => Key.Keypad6,        SilkKey.Keypad7        => Key.Keypad7,
        SilkKey.Keypad8        => Key.Keypad8,        SilkKey.Keypad9        => Key.Keypad9,
        SilkKey.KeypadDecimal  => Key.KeypadDecimal,  SilkKey.KeypadDivide   => Key.KeypadDivide,
        SilkKey.KeypadMultiply => Key.KeypadMultiply, SilkKey.KeypadSubtract => Key.KeypadSubtract,
        SilkKey.KeypadAdd      => Key.KeypadAdd,      SilkKey.KeypadEnter    => Key.KeypadEnter,
        SilkKey.KeypadEqual    => Key.KeypadEqual,
        SilkKey.ShiftLeft    => Key.ShiftLeft,   SilkKey.ControlLeft  => Key.ControlLeft,
        SilkKey.AltLeft      => Key.AltLeft,     SilkKey.SuperLeft    => Key.SuperLeft,
        SilkKey.ShiftRight   => Key.ShiftRight,  SilkKey.ControlRight => Key.ControlRight,
        SilkKey.AltRight     => Key.AltRight,    SilkKey.SuperRight   => Key.SuperRight,
        SilkKey.Menu         => Key.Menu,
        _                    => Key.Unknown,
    };

    private static SilkKey ToSilk(Key k) => k switch
    {
        Key.Space        => SilkKey.Space,        Key.Apostrophe   => SilkKey.Apostrophe,
        Key.Comma        => SilkKey.Comma,         Key.Minus        => SilkKey.Minus,
        Key.Period       => SilkKey.Period,        Key.Slash        => SilkKey.Slash,
        Key.Number0      => SilkKey.Number0,       Key.Number1      => SilkKey.Number1,
        Key.Number2      => SilkKey.Number2,       Key.Number3      => SilkKey.Number3,
        Key.Number4      => SilkKey.Number4,       Key.Number5      => SilkKey.Number5,
        Key.Number6      => SilkKey.Number6,       Key.Number7      => SilkKey.Number7,
        Key.Number8      => SilkKey.Number8,       Key.Number9      => SilkKey.Number9,
        Key.Semicolon    => SilkKey.Semicolon,     Key.Equal        => SilkKey.Equal,
        Key.A            => SilkKey.A,             Key.B            => SilkKey.B,
        Key.C            => SilkKey.C,             Key.D            => SilkKey.D,
        Key.E            => SilkKey.E,             Key.F            => SilkKey.F,
        Key.G            => SilkKey.G,             Key.H            => SilkKey.H,
        Key.I            => SilkKey.I,             Key.J            => SilkKey.J,
        Key.K            => SilkKey.K,             Key.L            => SilkKey.L,
        Key.M            => SilkKey.M,             Key.N            => SilkKey.N,
        Key.O            => SilkKey.O,             Key.P            => SilkKey.P,
        Key.Q            => SilkKey.Q,             Key.R            => SilkKey.R,
        Key.S            => SilkKey.S,             Key.T            => SilkKey.T,
        Key.U            => SilkKey.U,             Key.V            => SilkKey.V,
        Key.W            => SilkKey.W,             Key.X            => SilkKey.X,
        Key.Y            => SilkKey.Y,             Key.Z            => SilkKey.Z,
        Key.LeftBracket  => SilkKey.LeftBracket,   Key.BackSlash    => SilkKey.BackSlash,
        Key.RightBracket => SilkKey.RightBracket,  Key.GraveAccent  => SilkKey.GraveAccent,
        Key.Escape       => SilkKey.Escape,        Key.Enter        => SilkKey.Enter,
        Key.Tab          => SilkKey.Tab,           Key.Backspace    => SilkKey.Backspace,
        Key.Insert       => SilkKey.Insert,        Key.Delete       => SilkKey.Delete,
        Key.Right        => SilkKey.Right,         Key.Left         => SilkKey.Left,
        Key.Down         => SilkKey.Down,          Key.Up           => SilkKey.Up,
        Key.PageUp       => SilkKey.PageUp,        Key.PageDown     => SilkKey.PageDown,
        Key.Home         => SilkKey.Home,          Key.End          => SilkKey.End,
        Key.CapsLock     => SilkKey.CapsLock,      Key.ScrollLock   => SilkKey.ScrollLock,
        Key.NumLock      => SilkKey.NumLock,       Key.PrintScreen  => SilkKey.PrintScreen,
        Key.Pause        => SilkKey.Pause,
        Key.F1  => SilkKey.F1,  Key.F2  => SilkKey.F2,  Key.F3  => SilkKey.F3,
        Key.F4  => SilkKey.F4,  Key.F5  => SilkKey.F5,  Key.F6  => SilkKey.F6,
        Key.F7  => SilkKey.F7,  Key.F8  => SilkKey.F8,  Key.F9  => SilkKey.F9,
        Key.F10 => SilkKey.F10, Key.F11 => SilkKey.F11, Key.F12 => SilkKey.F12,
        Key.F13 => SilkKey.F13, Key.F14 => SilkKey.F14, Key.F15 => SilkKey.F15,
        Key.F16 => SilkKey.F16, Key.F17 => SilkKey.F17, Key.F18 => SilkKey.F18,
        Key.F19 => SilkKey.F19, Key.F20 => SilkKey.F20, Key.F21 => SilkKey.F21,
        Key.F22 => SilkKey.F22, Key.F23 => SilkKey.F23, Key.F24 => SilkKey.F24,
        Key.F25 => SilkKey.F25,
        Key.Keypad0        => SilkKey.Keypad0,        Key.Keypad1        => SilkKey.Keypad1,
        Key.Keypad2        => SilkKey.Keypad2,        Key.Keypad3        => SilkKey.Keypad3,
        Key.Keypad4        => SilkKey.Keypad4,        Key.Keypad5        => SilkKey.Keypad5,
        Key.Keypad6        => SilkKey.Keypad6,        Key.Keypad7        => SilkKey.Keypad7,
        Key.Keypad8        => SilkKey.Keypad8,        Key.Keypad9        => SilkKey.Keypad9,
        Key.KeypadDecimal  => SilkKey.KeypadDecimal,  Key.KeypadDivide   => SilkKey.KeypadDivide,
        Key.KeypadMultiply => SilkKey.KeypadMultiply, Key.KeypadSubtract => SilkKey.KeypadSubtract,
        Key.KeypadAdd      => SilkKey.KeypadAdd,      Key.KeypadEnter    => SilkKey.KeypadEnter,
        Key.KeypadEqual    => SilkKey.KeypadEqual,
        Key.ShiftLeft    => SilkKey.ShiftLeft,   Key.ControlLeft  => SilkKey.ControlLeft,
        Key.AltLeft      => SilkKey.AltLeft,     Key.SuperLeft    => SilkKey.SuperLeft,
        Key.ShiftRight   => SilkKey.ShiftRight,  Key.ControlRight => SilkKey.ControlRight,
        Key.AltRight     => SilkKey.AltRight,    Key.SuperRight   => SilkKey.SuperRight,
        Key.Menu         => SilkKey.Menu,
        _                => SilkKey.Unknown,
    };
}
