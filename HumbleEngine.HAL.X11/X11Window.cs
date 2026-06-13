namespace HumbleEngine.X11;

internal sealed class X11Window : Window, INativeWindowHandle, IClipboard
{
    private readonly IntPtr _display;
    private readonly ulong  _window;
    private readonly ulong  _wmDeleteWindow;
    private readonly ulong  _netWmName;
    private readonly ulong  _utf8String;

    // Clipboard (the CLIPBOARD selection): the atoms, the property we receive a
    // paste into, and the text we serve while we own the selection.
    private readonly ulong _clipboard;
    private readonly ulong _targets;
    private readonly ulong _clipboardProperty;
    private string? _ownedText;

    /// <summary>One pointing source per window — X11 core events erase physical provenance.</summary>
    private readonly Mouse _mouse = new();

    /// <summary>One keying source per window — core events merge physical keyboards.</summary>
    private readonly Keyboard _keyboard = new();
    private readonly byte[]   _keyTextBuffer = new byte[32];

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
                   EventMask.ButtonReleaseMask    |
                   EventMask.PointerMotionMask    |
                   EventMask.EnterWindowMask      |
                   EventMask.LeaveWindowMask));

        // Without WM_DELETE_WINDOW the window manager kills the X connection abruptly on close.
        _wmDeleteWindow = X11Native.XInternAtom(display, "WM_DELETE_WINDOW", false);
        X11Native.XSetWMProtocols(display, _window, ref _wmDeleteWindow, 1);

        _netWmName  = X11Native.XInternAtom(display, "_NET_WM_NAME", false);
        _utf8String = X11Native.XInternAtom(display, "UTF8_STRING", false);

        _clipboard         = X11Native.XInternAtom(display, "CLIPBOARD", false);
        _targets           = X11Native.XInternAtom(display, "TARGETS", false);
        _clipboardProperty = X11Native.XInternAtom(display, "HUMBLE_CLIPBOARD", false);

        if (desc.Borderless)
        {
            var hintsAtom = X11Native.XInternAtom(display, "_MOTIF_WM_HINTS", false);
            var hints = new MotifWmHints { Flags = 2 /* MWM_HINTS_DECORATIONS */, Decorations = 0 };
            X11Native.XChangeProperty(display, _window, hintsAtom, hintsAtom, 32, 0, ref hints, 5);
        }

        SetTitle(desc.Title);
        X11Native.XMapWindow(display, _window);
    }

    public override void PollEvents()
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

            case XEventType.MotionNotify:
                RaiseInput(new MouseMoved(_mouse, new Vector2(ev.xmotion.x, ev.xmotion.y)));
                break;

            case XEventType.ButtonPress:
            case XEventType.ButtonRelease:
                TranslateButton(ref ev);
                break;

            case XEventType.EnterNotify:
                RaiseInput(new MouseEntered(_mouse, new Vector2(ev.xcrossing.x, ev.xcrossing.y)));
                break;

            case XEventType.LeaveNotify:
                RaiseInput(new MouseExited(_mouse));
                break;

            case XEventType.KeyPress:
            case XEventType.KeyRelease:
                TranslateKey(ref ev);
                break;

            case XEventType.SelectionRequest:
                ServeSelection(ref ev.xselectionrequest);
                break;

            case XEventType.SelectionClear:
                _ownedText = null; // another application took the clipboard
                break;
        }
    }

    /// <summary>
    /// Translates a key event into the two channels: the logical key
    /// (keysym → <see cref="Key"/>) always, and on press the composed text
    /// when printable. X11 auto-repeats held keys — accepted as repeated
    /// presses.
    /// </summary>
    private void TranslateKey(ref XEvent ev)
    {
        var pressed = ev.type == XEventType.KeyPress;
        var count = X11Native.XLookupString(
            ref ev.xkey, _keyTextBuffer, _keyTextBuffer.Length, out var keysym, IntPtr.Zero);

        var key = KeysymTranslation.ToKey((uint)keysym);
        var modifiers = TranslateModifiers(ev.xkey.state);
        RaiseInput(pressed
            ? new KeyboardKeyPressed(_keyboard, key, modifiers)
            : new KeyboardKeyReleased(_keyboard, key, modifiers));

        if (!pressed || count <= 0)
            return;
        var text = System.Text.Encoding.Latin1.GetString(_keyTextBuffer, 0, count);
        if (text.Length > 0 && !char.IsControl(text[0]))
            RaiseInput(new KeyboardTextInput(_keyboard, text));
    }

    /// <summary>Core X modifier mask → HAL flags (Shift, Control, Mod1 = Alt, Mod4 = Super).</summary>
    private static KeyModifiers TranslateModifiers(uint state)
    {
        var modifiers = KeyModifiers.None;
        if ((state & 0x01) != 0) modifiers |= KeyModifiers.Shift;
        if ((state & 0x04) != 0) modifiers |= KeyModifiers.Ctrl;
        if ((state & 0x08) != 0) modifiers |= KeyModifiers.Alt;
        if ((state & 0x40) != 0) modifiers |= KeyModifiers.Super;
        return modifiers;
    }

    /// <summary>
    /// Translates an X button event: buttons 1-3 are real buttons; 4-7 are the
    /// scroll wheel (one press+release pair per notch — translated on press
    /// only, into our convention: +Y up, +X right).
    /// </summary>
    private void TranslateButton(ref XEvent ev)
    {
        var position = new Vector2(ev.xbutton.x, ev.xbutton.y);
        var pressed  = ev.type == XEventType.ButtonPress;

        switch (ev.xbutton.button)
        {
            case 1 or 2 or 3:
                var button = ev.xbutton.button switch
                {
                    1 => PointerButton.Left,
                    2 => PointerButton.Middle,
                    _ => PointerButton.Right,
                };
                RaiseInput(pressed
                    ? new MousePressed(_mouse, button, position)
                    : new MouseReleased(_mouse, button, position));
                break;

            case >= 4 and <= 7 when pressed:
                var delta = ev.xbutton.button switch
                {
                    4 => new Vector2(0f, 1f),
                    5 => new Vector2(0f, -1f),
                    6 => new Vector2(-1f, 0f),
                    _ => new Vector2(1f, 0f),
                };
                RaiseInput(new MouseScrolled(_mouse, delta, position));
                break;
        }
    }

    public override void Show()  => X11Native.XMapWindow(_display, _window);
    public override void Hide()  => X11Native.XUnmapWindow(_display, _window);

    /// <summary>
    /// Sets both title properties: legacy <c>WM_NAME</c> (Latin-1 only — anything
    /// beyond renders as boxes) and EWMH <c>_NET_WM_NAME</c> in UTF8_STRING,
    /// which every modern window manager prefers.
    /// </summary>
    public override void SetTitle(string title)
    {
        X11Native.XStoreName(_display, _window, title);
        var utf8 = System.Text.Encoding.UTF8.GetBytes(title);
        X11Native.XChangeProperty(
            _display, _window, _netWmName, _utf8String,
            8, 0 /* PropModeReplace */, utf8, utf8.Length);
        X11Native.XFlush(_display);
    }

    public override void Resize(int width, int height) =>
        X11Native.XResizeWindow(_display, _window, (uint)width, (uint)height);

    public override IWindow CreateChildWindow(WindowDescription description) =>
        new X11Window(Backend, _display, description);

    public IntPtr GetNativeHandle()     => new IntPtr((long)_window);
    public IntPtr GetConnectionHandle() => _display;

    // --- Clipboard (the CLIPBOARD selection) ---

    /// <inheritdoc/>
    public override IClipboard Clipboard => this;

    /// <summary>Becomes the CLIPBOARD owner and remembers the text to serve on request.</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _ownedText = text;
        X11Native.XSetSelectionOwner(_display, _clipboard, _window, X11Native.CurrentTime);
        X11Native.XFlush(_display);
    }

    /// <summary>
    /// Reads the CLIPBOARD: our own text when we own it, otherwise a conversion
    /// request to the owner, pumped (bounded) for the <c>SelectionNotify</c> answer.
    /// </summary>
    public string? GetText()
    {
        var owner = X11Native.XGetSelectionOwner(_display, _clipboard);
        if (owner == 0)
            return null;
        if (owner == _window)
            return _ownedText;

        X11Native.XConvertSelection(
            _display, _clipboard, _utf8String, _clipboardProperty, _window, X11Native.CurrentTime);
        X11Native.XFlush(_display);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        XEvent ev = default;
        while (clock.ElapsedMilliseconds < 500)
        {
            X11Native.XPending(_display); // read the connection into the event queue
            if (X11Native.XCheckTypedWindowEvent(_display, _window, XEventType.SelectionNotify, ref ev) != 0)
            {
                return ev.xselection.property == 0 ? null : ReadClipboardProperty();
            }
            System.Threading.Thread.Sleep(2);
        }
        return null;
    }

    /// <summary>Reads (and deletes) the delivered UTF-8 bytes from our clipboard property.</summary>
    private string? ReadClipboardProperty()
    {
        var status = X11Native.XGetWindowProperty(
            _display, _window, _clipboardProperty, 0, int.MaxValue / 4, true, 0 /* AnyPropertyType */,
            out _, out _, out var nitems, out _, out var data);
        if (status != 0 || data == IntPtr.Zero)
            return null;
        try
        {
            if (nitems == 0)
                return string.Empty;
            var bytes = new byte[nitems];
            System.Runtime.InteropServices.Marshal.Copy(data, bytes, 0, (int)nitems);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            X11Native.XFree(data);
        }
    }

    /// <summary>Answers a SelectionRequest: write the asked target into the requestor's property, then notify it.</summary>
    private void ServeSelection(ref XSelectionRequestEvent request)
    {
        ulong property = 0; // None = refused, until filled

        if (_ownedText is not null && request.selection == _clipboard)
        {
            if (request.target == _utf8String)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(_ownedText);
                X11Native.XChangeProperty(
                    _display, request.requestor, request.property, request.target, 8, 0, bytes, bytes.Length);
                property = request.property;
            }
            else if (request.target == _targets)
            {
                var atoms = new nint[] { (nint)_targets, (nint)_utf8String };
                X11Native.XChangeProperty(
                    _display, request.requestor, request.property, 4 /* XA_ATOM */, 32, 0, atoms, atoms.Length);
                property = request.property;
            }
        }

        var reply = new XEvent
        {
            xselection = new XSelectionEvent
            {
                type      = XEventType.SelectionNotify,
                display   = _display,
                requestor = request.requestor,
                selection = request.selection,
                target    = request.target,
                property  = property,
                time      = request.time,
            },
        };
        X11Native.XSendEvent(_display, request.requestor, false, 0, ref reply);
        X11Native.XFlush(_display);
    }

    private bool _disposed;

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        X11Native.XDestroyWindow(_display, _window);
        X11Native.XFlush(_display);
    }
}
