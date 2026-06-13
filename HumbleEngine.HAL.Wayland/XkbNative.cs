using System.Runtime.InteropServices;

namespace HumbleEngine.Wayland;

/// <summary>
/// P/Invoke surface of <c>libxkbcommon</c> — the keymap interpreter Wayland
/// mandates: the compositor only sends raw scancodes plus a keymap file
/// descriptor; turning them into keysyms, UTF-8 text and modifier state is
/// entirely the client's job (X11 did this server-side).
/// </summary>
internal static class XkbNative
{
    private const string Lib = "libxkbcommon.so.0";

    /// <summary>XKB_KEYMAP_FORMAT_TEXT_V1 — the only format Wayland sends.</summary>
    internal const int KeymapFormatTextV1 = 1;

    /// <summary>XKB_STATE_MODS_EFFECTIVE — depressed ∪ latched ∪ locked.</summary>
    internal const int StateModsEffective = 1 << 3;

    [DllImport(Lib)] internal static extern IntPtr xkb_context_new(int flags);
    [DllImport(Lib)] internal static extern void   xkb_context_unref(IntPtr context);

    [DllImport(Lib)] internal static extern IntPtr xkb_keymap_new_from_string(
        IntPtr context, IntPtr keymapText, int format, int flags);
    [DllImport(Lib)] internal static extern void   xkb_keymap_unref(IntPtr keymap);

    [DllImport(Lib)] internal static extern IntPtr xkb_state_new(IntPtr keymap);
    [DllImport(Lib)] internal static extern void   xkb_state_unref(IntPtr state);

    [DllImport(Lib)] internal static extern int xkb_state_update_mask(
        IntPtr state, uint depressedMods, uint latchedMods, uint lockedMods,
        uint depressedLayout, uint latchedLayout, uint lockedLayout);

    [DllImport(Lib)] internal static extern uint xkb_state_key_get_one_sym(IntPtr state, uint keycode);

    [DllImport(Lib)] internal static extern int xkb_state_key_get_utf8(
        IntPtr state, uint keycode, byte[] buffer, nuint size);

    [DllImport(Lib, CharSet = CharSet.Ansi)] internal static extern int xkb_state_mod_name_is_active(
        IntPtr state, string modName, int type);
}
