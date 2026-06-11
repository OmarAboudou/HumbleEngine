namespace HumbleEngine.X11;

/// <summary>
/// X11 windowing backend via P/Invoke on <c>libX11.so.6</c>.
/// </summary>
public sealed class X11WindowBackend : IWindowBackend
{
    private IntPtr _display;
    private bool   _initialized;

    /// <inheritdoc/>
    public string Name => "X11";

    /// <summary>
    /// Opens the connection to the X11 server. Idempotent.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The <c>DISPLAY</c> environment variable is not set or the server is unreachable.
    /// </exception>
    public void Initialize()
    {
        if (_initialized) return;
        _display = X11Native.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException(
                "Cannot open X11 display. Is the DISPLAY environment variable set?");
        _initialized = true;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public IWindow CreateWindow(WindowDescription description)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "X11WindowBackend is not initialised. Call Initialize() before CreateWindow().");

        return new X11Window(_display, description);
    }

    /// <summary>Closes the connection to the X11 server.</summary>
    public void Dispose()
    {
        if (!_initialized) return;
        X11Native.XCloseDisplay(_display);
        _display     = IntPtr.Zero;
        _initialized = false;
    }
}
