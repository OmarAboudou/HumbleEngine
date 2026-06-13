using System.Runtime.InteropServices;

namespace HumbleEngine.X11;

internal static class X11Native
{
    private const string Lib = "libX11.so.6";

    [DllImport(Lib)] internal static extern IntPtr XOpenDisplay(string? display);
    [DllImport(Lib)] internal static extern int    XCloseDisplay(IntPtr display);
    [DllImport(Lib)] internal static extern int    XDefaultScreen(IntPtr display);
    [DllImport(Lib)] internal static extern ulong  XRootWindow(IntPtr display, int screen);
    [DllImport(Lib)] internal static extern ulong  XBlackPixel(IntPtr display, int screen);

    [DllImport(Lib)] internal static extern ulong XCreateSimpleWindow(
        IntPtr display, ulong parent,
        int x, int y, uint width, uint height,
        uint borderWidth, ulong border, ulong background);

    [DllImport(Lib)] internal static extern int XDestroyWindow(IntPtr display, ulong window);
    [DllImport(Lib)] internal static extern int XSelectInput(IntPtr display, ulong window, long eventMask);
    [DllImport(Lib)] internal static extern int XMapWindow(IntPtr display, ulong window);
    [DllImport(Lib)] internal static extern int XUnmapWindow(IntPtr display, ulong window);
    [DllImport(Lib)] internal static extern int XStoreName(IntPtr display, ulong window, string name);
    [DllImport(Lib)] internal static extern int XResizeWindow(IntPtr display, ulong window, uint width, uint height);
    [DllImport(Lib)] internal static extern int XFlush(IntPtr display);
    [DllImport(Lib)] internal static extern int XPending(IntPtr display);
    [DllImport(Lib)] internal static extern int XNextEvent(IntPtr display, ref XEvent @event);

    [DllImport(Lib)] internal static extern ulong XInternAtom(
        IntPtr display, string atomName,
        [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [DllImport(Lib)] internal static extern int XSetWMProtocols(
        IntPtr display, ulong window, ref ulong protocols, int count);

    [DllImport(Lib)] internal static extern int XChangeProperty(
        IntPtr display, ulong window, ulong property, ulong type,
        int format, int mode, ref MotifWmHints data, int nelements);

    /// <summary>Byte-array overload — 8-bit format properties such as <c>_NET_WM_NAME</c> (UTF8_STRING).</summary>
    [DllImport(Lib)] internal static extern int XChangeProperty(
        IntPtr display, ulong window, ulong property, ulong type,
        int format, int mode, byte[] data, int nelements);

    /// <summary>
    /// Translates a key event into its keysym and its Latin-1 text (layout and
    /// shift applied). The Latin-1 limit is accepted for now — full UTF-8 text
    /// needs an X input context (XIM), deferred with composition/IME.
    /// </summary>
    [DllImport(Lib)] internal static extern int XLookupString(
        ref XKeyEvent keyEvent, byte[] buffer, int bufferSize,
        out ulong keysym, IntPtr composeStatus);
}

/// <summary>
/// Used with <c>_MOTIF_WM_HINTS</c> to strip window decorations.
/// Each field maps to an X11 <c>long</c> (8 bytes on 64-bit; X reads only the lower 32 bits).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MotifWmHints
{
    public long Flags;        // 2 = MWM_HINTS_DECORATIONS
    public long Functions;
    public long Decorations;  // 0 = none, 1 = all
    public long InputMode;
    public long Status;
}

[Flags]
internal enum EventMask : long
{
    KeyPressMask        = 1L << 0,
    KeyReleaseMask      = 1L << 1,
    ButtonPressMask     = 1L << 2,
    ButtonReleaseMask   = 1L << 3,
    EnterWindowMask     = 1L << 4,
    LeaveWindowMask     = 1L << 5,
    PointerMotionMask   = 1L << 6,
    ExposureMask        = 1L << 15,
    StructureNotifyMask = 1L << 17,
}

internal static class XEventType
{
    public const int KeyPress        = 2;
    public const int KeyRelease      = 3;
    public const int ButtonPress     = 4;
    public const int ButtonRelease   = 5;
    public const int MotionNotify    = 6;
    public const int EnterNotify     = 7;
    public const int LeaveNotify     = 8;
    public const int Expose          = 12;
    public const int DestroyNotify   = 17;
    public const int ConfigureNotify = 22;
    public const int ClientMessage   = 33;
}

/// <summary>
/// C union mapping the first 192 bytes of an X11 event (64-bit Linux layout).
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct XEvent
{
    [FieldOffset(0)] public int                 type;
    [FieldOffset(0)] public XClientMessageEvent xclient;
    [FieldOffset(0)] public XConfigureEvent     xconfigure;
    [FieldOffset(0)] public XButtonEvent        xbutton;
    [FieldOffset(0)] public XMotionEvent        xmotion;
    [FieldOffset(0)] public XCrossingEvent      xcrossing;
    [FieldOffset(0)] public XKeyEvent           xkey;
}

/// <summary>XKeyEvent field offsets for 64-bit Linux.</summary>
[StructLayout(LayoutKind.Explicit, Size = 96)]
internal struct XKeyEvent
{
    [FieldOffset(0)]  public int   type;
    [FieldOffset(56)] public ulong time;
    [FieldOffset(64)] public int   x;
    [FieldOffset(68)] public int   y;
    [FieldOffset(80)] public uint  state;
    [FieldOffset(84)] public uint  keycode;
}

/// <summary>XButtonEvent field offsets for 64-bit Linux. Buttons 4-7 are the scroll wheel.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct XButtonEvent
{
    [FieldOffset(0)]  public int   type;
    [FieldOffset(56)] public ulong time;
    [FieldOffset(64)] public int   x;
    [FieldOffset(68)] public int   y;
    [FieldOffset(80)] public uint  state;
    [FieldOffset(84)] public uint  button;
}

/// <summary>XMotionEvent field offsets for 64-bit Linux.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct XMotionEvent
{
    [FieldOffset(0)]  public int   type;
    [FieldOffset(56)] public ulong time;
    [FieldOffset(64)] public int   x;
    [FieldOffset(68)] public int   y;
    [FieldOffset(80)] public uint  state;
}

/// <summary>XCrossingEvent (Enter/LeaveNotify) field offsets for 64-bit Linux.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct XCrossingEvent
{
    [FieldOffset(0)]  public int   type;
    [FieldOffset(56)] public ulong time;
    [FieldOffset(64)] public int   x;
    [FieldOffset(68)] public int   y;
}

/// <summary>
/// XClientMessageEvent field offsets for 64-bit Linux
/// (<c>long</c> = 8 bytes, <c>Bool</c> = <c>int</c> = 4 bytes).
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct XClientMessageEvent
{
    [FieldOffset(0)]  public int    type;
    [FieldOffset(8)]  public ulong  serial;
    [FieldOffset(16)] public int    send_event;
    [FieldOffset(24)] public IntPtr display;
    [FieldOffset(32)] public ulong  window;
    [FieldOffset(40)] public ulong  message_type;
    [FieldOffset(48)] public int    format;
    // data.l[0..4] — 40-byte union; only l[0] is needed for WM_DELETE_WINDOW
    [FieldOffset(56)] public long   l0;
    [FieldOffset(64)] public long   l1;
    [FieldOffset(72)] public long   l2;
    [FieldOffset(80)] public long   l3;
    [FieldOffset(88)] public long   l4;
}

[StructLayout(LayoutKind.Explicit)]
internal struct XConfigureEvent
{
    [FieldOffset(0)]  public int    type;
    [FieldOffset(8)]  public ulong  serial;
    [FieldOffset(16)] public int    send_event;
    [FieldOffset(24)] public IntPtr display;
    [FieldOffset(32)] public ulong  @event;
    [FieldOffset(40)] public ulong  window;
    [FieldOffset(48)] public int    x;
    [FieldOffset(52)] public int    y;
    [FieldOffset(56)] public int    width;
    [FieldOffset(60)] public int    height;
    [FieldOffset(64)] public int    border_width;
    [FieldOffset(72)] public ulong  above;
    [FieldOffset(80)] public int    override_redirect;
}
