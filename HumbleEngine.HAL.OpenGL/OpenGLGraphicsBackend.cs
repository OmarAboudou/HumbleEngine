namespace HumbleEngine.OpenGL;

/// <summary>
/// OpenGL graphics backend using GLX on Linux/X11.
/// Compatible with <see cref="X11WindowBackend"/> and <see cref="WaylandWindowBackend"/>.
/// </summary>
public sealed class OpenGLGraphicsBackend : IGraphicsBackend
{
    private bool _initialized;

    /// <inheritdoc/>
    public string Name => "OpenGL";

    /// <inheritdoc/>
    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    /// <summary>
    /// Verifies that GLX 1.2 or later is available on the default X11 display.
    /// Opens a temporary display connection for the check only.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The X11 display cannot be opened, or the GLX version is too old.
    /// </exception>
    public void Initialize()
    {
        if (_initialized) return;

        var display = GLXNative.XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException(
                "Cannot open X11 display to verify GLX. Is DISPLAY set?");

        try
        {
            if (GLXNative.glXQueryVersion(display, out int major, out int minor) == 0)
                throw new InvalidOperationException("GLX is not available on this display.");

            if (major < 1 || (major == 1 && minor < 2))
                throw new InvalidOperationException(
                    $"GLX 1.2 or later required, server reports {major}.{minor}.");
        }
        finally
        {
            GLXNative.XCloseDisplay(display);
        }

        _initialized = true;
    }

    /// <summary>
    /// Creates an OpenGL renderer for the given surface by establishing a GLX context.
    /// The surface must implement <see cref="INativeWindowHandle"/> with a valid X11 display connection.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    /// <exception cref="ArgumentException">
    /// The surface does not expose native handles, or the connection handle is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No GLX visual matching the required attributes could be found,
    /// or the GLX context could not be created or made current.
    /// </exception>
    public IRenderer CreateRenderer(IGraphicsSurface surface)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "OpenGLGraphicsBackend not initialised. Call Initialize() first.");

        if (surface is not INativeWindowHandle handle)
            throw new ArgumentException(
                "Surface does not expose native handles.", nameof(surface));

        var display = handle.GetConnectionHandle();
        var window  = (ulong)handle.GetNativeHandle().ToInt64();

        if (display == IntPtr.Zero)
            throw new ArgumentException(
                "Surface has no X11 display connection. GLX requires an X11 window.", nameof(surface));

        int screen = GLXNative.XDefaultScreen(display);

        int[] attribs =
        [
            GLXAttrib.Rgba,
            GLXAttrib.DoubleBuffer,
            GLXAttrib.DepthSize, 24,
            GLXAttrib.None
        ];

        var visualInfo = GLXNative.glXChooseVisual(display, screen, attribs);
        if (visualInfo == IntPtr.Zero)
            throw new InvalidOperationException(
                "glXChooseVisual found no visual matching RGBA + double-buffer + depth-24.");

        IntPtr context;
        try
        {
            context = GLXNative.glXCreateContext(display, visualInfo, IntPtr.Zero, direct: true);
        }
        finally
        {
            GLXNative.XFree(visualInfo);
        }

        if (context == IntPtr.Zero)
            throw new InvalidOperationException(
                "glXCreateContext failed. Direct rendering may not be available.");

        if (!GLXNative.glXMakeCurrent(display, window, context))
        {
            GLXNative.glXDestroyContext(display, context);
            throw new InvalidOperationException(
                "glXMakeCurrent failed. The window visual may be incompatible with the GLX context.");
        }

        return new OpenGLRenderer(display, window, context);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _initialized = false;
    }
}
