namespace HumbleEngine.X11;

internal sealed class X11Window : Window, INativeWindowHandle
{
    private readonly IntPtr _display;
    private readonly ulong  _window;
    private readonly ulong  _wmDeleteWindow;

    internal X11Window(IWindowBackend backend, IntPtr display, WindowDescription desc)
        : base(backend)
    {
        _display = display;
        Width    = desc.Width;
        Height   = desc.Height;
        int screen = X11Native.XDefaultScreen(display);

        _window = X11Native.XCreateSimpleWindow(
            display,
            X11Native.XRootWindow(display, screen),
            0, 0,
            (uint)desc.Width, (uint)desc.Height,
            1,
            X11Native.XBlackPixel(display, screen),
            X11Native.XBlackPixel(display, screen));

        X11Native.XSelectInput(display, _window,
            (long)(EventMask.ExposureMask        |
                   EventMask.StructureNotifyMask  |
                   EventMask.KeyPressMask         |
                   EventMask.KeyReleaseMask       |
                   EventMask.ButtonPressMask      |
                   EventMask.ButtonReleaseMask));

        // Without WM_DELETE_WINDOW the window manager kills the X connection abruptly on close.
        _wmDeleteWindow = X11Native.XInternAtom(display, "WM_DELETE_WINDOW", false);
        X11Native.XSetWMProtocols(display, _window, ref _wmDeleteWindow, 1);

        if (desc.Borderless)
        {
            var hintsAtom = X11Native.XInternAtom(display, "_MOTIF_WM_HINTS", false);
            var hints = new MotifWmHints { Flags = 2 /* MWM_HINTS_DECORATIONS */, Decorations = 0 };
            X11Native.XChangeProperty(display, _window, hintsAtom, hintsAtom, 32, 0, ref hints, 5);
        }

        X11Native.XStoreName(display, _window, desc.Title);
        X11Native.XMapWindow(display, _window);
    }

    protected override void PollEvents()
    {
        while (X11Native.XPending(_display) > 0)
        {
            XEvent ev = default;
            X11Native.XNextEvent(_display, ref ev);
            HandleEvent(ref ev);
        }
    }

    private void HandleEvent(ref XEvent ev)
    {
        switch (ev.type)
        {
            case XEventType.ClientMessage:
                if (ev.xclient.l0 == (long)_wmDeleteWindow)
                {
                    ShouldClose = true;
                    RaiseClose();
                }
                break;

            case XEventType.ConfigureNotify:
                RaiseResize(ev.xconfigure.width, ev.xconfigure.height);
                break;

            case XEventType.DestroyNotify:
                ShouldClose = true;
                break;
        }
    }

    public override void Show()  => X11Native.XMapWindow(_display, _window);
    public override void Hide()  => X11Native.XUnmapWindow(_display, _window);

    public override void SetTitle(string title) =>
        X11Native.XStoreName(_display, _window, title);

    public override void Resize(int width, int height) =>
        X11Native.XResizeWindow(_display, _window, (uint)width, (uint)height);

    public override IWindow CreateChildWindow(WindowDescription description) =>
        new X11Window(Backend, _display, description);

    public IntPtr GetNativeHandle()     => new IntPtr((long)_window);
    public IntPtr GetConnectionHandle() => _display;

    private bool _disposed;

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        X11Native.XDestroyWindow(_display, _window);
        X11Native.XFlush(_display);
    }
}
