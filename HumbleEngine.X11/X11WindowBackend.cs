namespace HumbleEngine.X11;

public sealed class X11WindowBackend : IWindowBackend
{
    private IntPtr _display;
    private bool   _initialized;

    public string Name => "X11";

    public void Initialize()
    {
        if (_initialized) return;
        _display = X11Native.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException(
                "Impossible d'ouvrir le display X11. La variable DISPLAY est-elle définie ?");
        _initialized = true;
    }

    public IWindow CreateWindow(WindowDescription description)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "X11WindowBackend non initialisé. Appelez Initialize() avant CreateWindow().");

        return new X11Window(_display, description);
    }

    public void Dispose()
    {
        if (!_initialized) return;
        X11Native.XCloseDisplay(_display);
        _display     = IntPtr.Zero;
        _initialized = false;
    }
}
