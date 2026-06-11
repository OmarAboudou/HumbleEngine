namespace HumbleEngine.Wayland;

/// <summary>
/// Wayland windowing backend via P/Invoke on <c>libwayland-client.so.0</c>.
/// Uses the XDG Shell protocol for top-level window management.
/// </summary>
public sealed class WaylandWindowBackend : IWindowBackend
{
    private IntPtr _display;
    private bool   _initialized;

    /// <inheritdoc/>
    public string Name => "Wayland";

    /// <summary>
    /// Connects to the Wayland compositor.
    /// Reads <c>WAYLAND_DISPLAY</c> (defaults to <c>wayland-0</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No Wayland compositor socket is available.
    /// </exception>
    public void Initialize()
    {
        if (_initialized) return;
        _display = WaylandNative.wl_display_connect(null);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException(
                "Cannot connect to Wayland compositor. Is WAYLAND_DISPLAY set?");
        _initialized = true;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public IWindow CreateWindow(WindowDescription description)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "WaylandWindowBackend is not initialised. Call Initialize() before CreateWindow().");
        return new WaylandWindow(_display, description);
    }

    /// <summary>Disconnects from the Wayland compositor.</summary>
    public void Dispose()
    {
        if (!_initialized) return;
        WaylandNative.wl_display_disconnect(_display);
        _display     = IntPtr.Zero;
        _initialized = false;
    }
}
