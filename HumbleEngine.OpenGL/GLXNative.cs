using System.Runtime.InteropServices;

namespace HumbleEngine.OpenGL;

internal static class GLXNative
{
    private const string LibGL  = "libGL.so.1";
    private const string LibX11 = "libX11.so.6";

    // --- X11 helpers (duplicated here to keep OpenGL self-contained) ---

    [DllImport(LibX11)] internal static extern IntPtr XOpenDisplay(string? display);
    [DllImport(LibX11)] internal static extern int    XCloseDisplay(IntPtr display);
    [DllImport(LibX11)] internal static extern int    XDefaultScreen(IntPtr display);

    /// <summary>Releases memory allocated by X11 (e.g. the XVisualInfo returned by glXChooseVisual).</summary>
    [DllImport(LibX11)] internal static extern void XFree(IntPtr data);

    // --- GLX ---

    /// <summary>
    /// Queries the GLX version supported by the server.
    /// Returns non-zero on success.
    /// </summary>
    [DllImport(LibGL)]
    internal static extern int glXQueryVersion(IntPtr display, out int major, out int minor);

    /// <summary>
    /// Returns a pointer to an XVisualInfo matching the given attribute list,
    /// or <see cref="IntPtr.Zero"/> if no match exists.
    /// The caller must free the returned pointer with <see cref="XFree"/>.
    /// </summary>
    [DllImport(LibGL)]
    internal static extern IntPtr glXChooseVisual(IntPtr display, int screen, int[] attribList);

    /// <summary>
    /// Creates a GLX rendering context.
    /// Pass <paramref name="direct"/>=<c>true</c> to request hardware-accelerated direct rendering.
    /// </summary>
    [DllImport(LibGL)]
    internal static extern IntPtr glXCreateContext(
        IntPtr display,
        IntPtr visualInfo,
        IntPtr shareList,
        [MarshalAs(UnmanagedType.Bool)] bool direct);

    /// <summary>Destroys a GLX rendering context.</summary>
    [DllImport(LibGL)]
    internal static extern void glXDestroyContext(IntPtr display, IntPtr context);

    /// <summary>
    /// Binds a GLX context to a drawable (window).
    /// Pass <paramref name="drawable"/>=0 and <paramref name="context"/>=<see cref="IntPtr.Zero"/>
    /// to release the current context.
    /// </summary>
    [DllImport(LibGL)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool glXMakeCurrent(IntPtr display, ulong drawable, IntPtr context);

    /// <summary>Swaps the front and back buffers of the given drawable.</summary>
    [DllImport(LibGL)]
    internal static extern void glXSwapBuffers(IntPtr display, ulong drawable);

    // --- OpenGL ---

    /// <summary>Sets the RGBA values used by subsequent <see cref="glClear"/> calls.</summary>
    [DllImport(LibGL)]
    internal static extern void glClearColor(float r, float g, float b, float a);

    /// <summary>Clears the specified buffer(s) to their preset clear values.</summary>
    [DllImport(LibGL)]
    internal static extern void glClear(uint mask);

    /// <summary>Forces execution of all pending GL commands.</summary>
    [DllImport(LibGL)]
    internal static extern void glFlush();

    /// <summary>Sets the viewport rectangle in window coordinates.</summary>
    [DllImport(LibGL)]
    internal static extern void glViewport(int x, int y, int width, int height);
}

/// <summary>GLX visual attribute tokens for the <c>glXChooseVisual</c> attribute list.</summary>
internal static class GLXAttrib
{
    /// <summary>Use true-colour (RGBA) rendering.</summary>
    public const int Rgba         = 4;
    /// <summary>Request a double-buffered visual.</summary>
    public const int DoubleBuffer = 5;
    /// <summary>Minimum depth-buffer bit depth (followed by the desired bit count).</summary>
    public const int DepthSize    = 12;
    /// <summary>Terminates the attribute list.</summary>
    public const int None         = 0;
}

/// <summary>OpenGL bitmask constants for <c>glClear</c>.</summary>
internal static class GLMask
{
    public const uint ColorBuffer = 0x00004000;
    public const uint DepthBuffer = 0x00000100;
}
