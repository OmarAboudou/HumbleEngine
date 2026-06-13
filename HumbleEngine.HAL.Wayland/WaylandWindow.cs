using System.Runtime.InteropServices;

namespace HumbleEngine.Wayland;

/// <summary>
/// Wayland desktop window. Supports two decoration paths:
/// <list type="bullet">
///   <item><term>libdecor</term><description>
///     Used when <c>libdecor-0</c> is installed and <see cref="WindowDescription.Borderless"/> is false.
///     libdecor draws client-side decorations (CSD) — required on GNOME/Mutter which refuses to
///     expose <c>zxdg_decoration_manager_v1</c>. libdecor internally owns <c>xdg_surface</c>
///     and <c>xdg_toplevel</c>.
///   </description></item>
///   <item><term>Raw XDG Shell</term><description>
///     Fallback when libdecor is absent or the window is borderless. Requests server-side
///     decorations via <c>zxdg_decoration_manager_v1</c> when the compositor exposes it
///     (KDE/Sway/wlroots). On GNOME the window stays borderless.
///   </description></item>
/// </list>
/// </summary>
internal sealed class WaylandWindow : Window, INativeWindowHandle, IClipboard
{
    // --- Common Wayland objects ---
    private readonly IntPtr _display;
    private          IntPtr _registry;
    private          IntPtr _compositor;
    private          IntPtr _surface;

    // --- Shared-memory buffer ---
    private IntPtr _wlShm;
    private IntPtr _buffer;
    private int    _shmFd   = -1;
    private IntPtr _shmData = IntPtr.Zero;
    private int    _shmSize;

    // --- Built-in interface pointers (fetched from libwayland-client.so) ---
    private static readonly IntPtr RegistryIface   = WaylandNative.GetBuiltinInterface("wl_registry_interface");
    private static readonly IntPtr CompositorIface = WaylandNative.GetBuiltinInterface("wl_compositor_interface");
    private static readonly IntPtr SurfaceIface    = WaylandNative.GetBuiltinInterface("wl_surface_interface");
    private static readonly IntPtr ShmIface        = WaylandNative.GetBuiltinInterface("wl_shm_interface");
    private static readonly IntPtr ShmPoolIface    = WaylandNative.GetBuiltinInterface("wl_shm_pool_interface");
    private static readonly IntPtr BufferIface     = WaylandNative.GetBuiltinInterface("wl_buffer_interface");
    private static readonly IntPtr SeatIface       = WaylandNative.GetBuiltinInterface("wl_seat_interface");
    private static readonly IntPtr PointerIface    = WaylandNative.GetBuiltinInterface("wl_pointer_interface");
    private static readonly IntPtr KeyboardIface   = WaylandNative.GetBuiltinInterface("wl_keyboard_interface");
    private static readonly IntPtr DataDeviceManagerIface = WaylandNative.GetBuiltinInterface("wl_data_device_manager_interface");
    private static readonly IntPtr DataDeviceIface = WaylandNative.GetBuiltinInterface("wl_data_device_interface");
    private static readonly IntPtr DataSourceIface = WaylandNative.GetBuiltinInterface("wl_data_source_interface");
    private static readonly IntPtr DataOfferIface  = WaylandNative.GetBuiltinInterface("wl_data_offer_interface");

    // --- Input (wl_seat, bound at version 1: five pointer events, no frame batching) ---
    /// <summary>One pointing source per window — the Wayland seat aggregates physical devices.</summary>
    private readonly Mouse _mouse = new();
    private IntPtr _seat;
    private IntPtr _pointer;
    /// <summary>The pointer is over <b>our</b> surface — libdecor's decoration surfaces share the seat.</summary>
    private bool   _pointerInside;
    private double _pointerX;
    private double _pointerY;

    // --- Keyboard (wl_keyboard v1 + xkbcommon: the compositor sends scancodes
    // --- and a keymap fd; keysyms, UTF-8 and modifiers are the client's job) ---
    /// <summary>One keying source per window — the seat merges physical keyboards.</summary>
    private readonly Keyboard _keyboardDevice = new();
    private readonly byte[]   _keyTextBuffer  = new byte[32];
    private IntPtr _keyboard;
    private bool   _keyboardInside;
    private IntPtr _xkbContext;
    private IntPtr _xkbKeymap;
    private IntPtr _xkbState;

    // Client-side key repeat. Wayland v1 has no repeat_info (a v4 event), so the
    // cadence is a sensible default rather than the compositor's configured rate
    // (honouring repeat_info would need a v4 seat binding — deferred). The held
    // key's events are stored and replayed from PollEvents after the delay, until
    // it is released — X11 gets this from the server natively.
    private const long RepeatDelayMs    = 400;
    private const long RepeatIntervalMs = 33;
    private uint   _repeatScancode;
    private bool   _repeating;
    private KeyboardKeyPressed? _repeatKey;
    private string? _repeatText;
    private readonly System.Diagnostics.Stopwatch _repeatClock = new();
    private long _lastRepeatMs;

    // --- Clipboard (wl_data_device) ---
    private const string ClipboardMime = "text/plain;charset=utf-8";
    private IntPtr _dataDeviceManager;
    private IntPtr _dataDevice;
    private IntPtr _dataSource;     // the source we own while holding the clipboard
    private IntPtr _currentOffer;   // the offer the compositor advertises for pasting
    private uint   _lastSerial;     // the most recent input serial — set_selection needs it
    private string? _clipboardText; // the text our source serves
    private GCHandle _dataDeviceListenerHandle;
    private GCHandle _dataSourceListenerHandle;

    /// <summary>True while the compositor reports the surface suspended — the loop pumps but skips rendering.</summary>
    private bool _suspended;

    // --- Decoration path selector ---
    private readonly bool _useLibdecor;
    private readonly int  _defaultWidth;
    private readonly int  _defaultHeight;

    // =========================================================================
    // Raw XDG Shell path fields
    // =========================================================================

    private IntPtr _xdgWmBase;
    private IntPtr _xdgSurface;
    private IntPtr _xdgToplevel;
    private IntPtr _decorationManager;
    private IntPtr _toplevelDecoration;

    private static readonly IntPtr XdgWmBaseName = Marshal.ReadIntPtr(XdgInterfaces.XdgWmBase, 0);

    private readonly WlRegistryGlobal       _onGlobal;
    private readonly WlRegistryGlobalRemove _onGlobalRemove;
    private readonly XdgWmBasePing          _onWmBasePing;
    private readonly XdgSurfaceConfigure    _onSurfaceConfigure;
    private readonly XdgToplevelConfigure   _onToplevelConfigure;
    private readonly XdgToplevelClose       _onToplevelClose;
    private readonly WlSeatCapabilities     _onSeatCapabilities;
    private readonly WlSeatName             _onSeatName;
    private readonly WlPointerEnter         _onPointerEnter;
    private readonly WlPointerLeave         _onPointerLeave;
    private readonly WlPointerMotion        _onPointerMotion;
    private readonly WlPointerButton        _onPointerButton;
    private readonly WlPointerAxis          _onPointerAxis;
    private readonly WlKeyboardKeymap       _onKeyboardKeymap;
    private readonly WlKeyboardEnter        _onKeyboardEnter;
    private readonly WlKeyboardLeave        _onKeyboardLeave;
    private readonly WlKeyboardKey          _onKeyboardKey;
    private readonly WlKeyboardModifiers    _onKeyboardModifiers;
    private readonly WlDataDeviceDataOffer  _onDataOffer;
    private readonly WlDataDeviceEnter      _onDataEnter;
    private readonly WlDataDeviceLeave      _onDataLeave;
    private readonly WlDataDeviceMotion     _onDataMotion;
    private readonly WlDataDeviceDrop       _onDataDrop;
    private readonly WlDataDeviceSelection  _onDataSelection;
    private readonly WlDataSourceTarget     _onSourceTarget;
    private readonly WlDataSourceSend       _onSourceSend;
    private readonly WlDataSourceCancelled  _onSourceCancelled;

    private GCHandle _registryListenerHandle;
    private GCHandle _wmBaseListenerHandle;
    private GCHandle _xdgSurfaceListenerHandle;
    private GCHandle _toplevelListenerHandle;
    private GCHandle _seatListenerHandle;
    private GCHandle _pointerListenerHandle;
    private GCHandle _keyboardListenerHandle;

    // Pending configure state (used by both paths)
    private uint _pendingSerial;
    private int  _pendingWidth;
    private int  _pendingHeight;
    private bool _configureReceived;

    // Once a renderer (Vulkan…) owns the surface content, the window must stop
    // attaching its shm placeholder buffer — two buffer sources would fight.
    private bool _rendererOwnsSurface;

    // =========================================================================
    // libdecor path fields
    // =========================================================================

    private IntPtr _libdecorCtx;
    private IntPtr _libdecorFrame;

    private GCHandle _libdecorContextListenerHandle;
    private GCHandle _libdecorFrameListenerHandle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LibdecorErrorDelegate(IntPtr ctx, int error, IntPtr message);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LibdecorFrameConfigureDelegate(IntPtr frame, IntPtr config, IntPtr userData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LibdecorFrameCloseDelegate(IntPtr frame, IntPtr userData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LibdecorFrameCommitDelegate(IntPtr frame, IntPtr userData);

    private LibdecorErrorDelegate?          _onLibdecorError;
    private LibdecorFrameConfigureDelegate? _onLibdecorConfigure;
    private LibdecorFrameCloseDelegate?     _onLibdecorClose;
    private LibdecorFrameCommitDelegate?    _onLibdecorCommit;

    // =========================================================================
    // Constructor
    // =========================================================================

    internal WaylandWindow(IWindowBackend backend, IntPtr display, WindowDescription desc)
        : base(backend)
    {
        _display       = display;
        _defaultWidth  = desc.Width;
        _defaultHeight = desc.Height;
        _useLibdecor   = !desc.Borderless && LibDecorNative.IsAvailable;

        // Wire up all delegates before any roundtrip so GC cannot collect them.
        _onGlobal            = OnRegistryGlobal;
        _onGlobalRemove      = OnRegistryGlobalRemove;
        _onWmBasePing        = OnWmBasePing;
        _onSurfaceConfigure  = OnXdgSurfaceConfigure;
        _onToplevelConfigure = OnXdgToplevelConfigure;
        _onToplevelClose     = OnXdgToplevelClose;
        _onSeatCapabilities  = OnSeatCapabilities;
        _onSeatName          = OnSeatName;
        _onPointerEnter      = OnPointerEnter;
        _onPointerLeave      = OnPointerLeave;
        _onPointerMotion     = OnPointerMotion;
        _onPointerButton     = OnPointerButton;
        _onPointerAxis       = OnPointerAxis;
        _onKeyboardKeymap    = OnKeyboardKeymap;
        _onKeyboardEnter     = OnKeyboardEnter;
        _onKeyboardLeave     = OnKeyboardLeave;
        _onKeyboardKey       = OnKeyboardKey;
        _onKeyboardModifiers = OnKeyboardModifiers;
        _onDataOffer         = OnDataOffer;
        _onDataEnter         = OnDataEnter;
        _onDataLeave         = OnDataLeave;
        _onDataMotion        = OnDataMotion;
        _onDataDrop          = OnDataDrop;
        _onDataSelection     = OnDataSelection;
        _onSourceTarget      = OnSourceTarget;
        _onSourceSend        = OnSourceSend;
        _onSourceCancelled   = OnSourceCancelled;

        // 1. Bind wl_registry and discover globals.
        _registry = WaylandNative.MarshalNew(display, Op.DisplayGetRegistry, RegistryIface, IntPtr.Zero);
        AddListener(_registry, [
            Marshal.GetFunctionPointerForDelegate(_onGlobal),
            Marshal.GetFunctionPointerForDelegate(_onGlobalRemove),
        ], out _registryListenerHandle);

        WaylandNative.wl_display_roundtrip(display);

        if (_compositor == IntPtr.Zero)
            throw new InvalidOperationException(
                "Wayland compositor (wl_compositor) not announced.");
        if (_wlShm == IntPtr.Zero)
            throw new InvalidOperationException(
                "wl_shm not announced — cannot create pixel buffer.");

        if (!_useLibdecor)
        {
            if (_xdgWmBase == IntPtr.Zero)
                throw new InvalidOperationException(
                    "xdg_wm_base not announced. Is the compositor XDG-Shell-compliant?");

            AddListener(_xdgWmBase, [
                Marshal.GetFunctionPointerForDelegate(_onWmBasePing),
            ], out _wmBaseListenerHandle);
        }

        // 2. Create wl_surface (common to both paths).
        _surface = WaylandNative.MarshalNew(_compositor, Op.CompositorCreateSurface, SurfaceIface, IntPtr.Zero);

        if (_useLibdecor)
            InitWithLibdecor(desc);
        else
            InitWithXdg(desc);

        // The initial configure may have imposed a size; later resizes go through RaiseResize.
        Width  = _pendingWidth  > 0 ? _pendingWidth  : desc.Width;
        Height = _pendingHeight > 0 ? _pendingHeight : desc.Height;
    }

    // =========================================================================
    // Init — libdecor path
    // =========================================================================

    private void InitWithLibdecor(WindowDescription desc)
    {
        // Context vtable: libdecor_interface has 1 real callback (error) + 10 reserved nulls.
        _onLibdecorError = OnLibdecorError;
        var ctxVtable = new IntPtr[11];
        ctxVtable[0] = Marshal.GetFunctionPointerForDelegate(_onLibdecorError);
        _libdecorContextListenerHandle = GCHandle.Alloc(ctxVtable, GCHandleType.Pinned);

        _libdecorCtx = LibDecorNative.libdecor_new(_display, _libdecorContextListenerHandle.AddrOfPinnedObject());
        if (_libdecorCtx == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create libdecor context.");

        // Frame vtable: libdecor_frame_interface has configure, close, commit,
        // dismiss_popup (null), + 9 reserved nulls — 13 pointers total.
        _onLibdecorConfigure = OnLibdecorConfigure;
        _onLibdecorClose     = OnLibdecorClose;
        _onLibdecorCommit    = OnLibdecorCommit;
        var frameVtable = new IntPtr[13];
        frameVtable[0] = Marshal.GetFunctionPointerForDelegate(_onLibdecorConfigure);
        frameVtable[1] = Marshal.GetFunctionPointerForDelegate(_onLibdecorClose);
        frameVtable[2] = Marshal.GetFunctionPointerForDelegate(_onLibdecorCommit);
        // [3] dismiss_popup = null; [4..12] reserved = null
        _libdecorFrameListenerHandle = GCHandle.Alloc(frameVtable, GCHandleType.Pinned);

        // libdecor takes over xdg_surface/xdg_toplevel; we pass IntPtr.Zero as userData
        // because the delegates already capture `this` via the C# delegate mechanism.
        _libdecorFrame = LibDecorNative.libdecor_decorate(
            _libdecorCtx, _surface,
            _libdecorFrameListenerHandle.AddrOfPinnedObject(),
            IntPtr.Zero);
        if (_libdecorFrame == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create libdecor frame.");

        LibDecorNative.libdecor_frame_set_title(_libdecorFrame, desc.Title);
        LibDecorNative.libdecor_frame_map(_libdecorFrame);

        // Pump events until the mandatory initial configure fires (creates the shm buffer).
        int limit = 200;
        while (!_configureReceived && limit-- > 0)
            LibDecorNative.libdecor_dispatch(_libdecorCtx, 16);

        if (!_configureReceived)
            throw new InvalidOperationException("No libdecor configure received after initial map.");

        WaylandNative.wl_display_flush(_display);
    }

    // =========================================================================
    // Init — raw XDG Shell path
    // =========================================================================

    private void InitWithXdg(WindowDescription desc)
    {
        _xdgSurface = WaylandNative.MarshalNewWithObj(
            _xdgWmBase, Op.XdgWmBaseGetXdgSurface,
            XdgInterfaces.XdgSurface, 1,
            IntPtr.Zero, _surface);

        AddListener(_xdgSurface, [
            Marshal.GetFunctionPointerForDelegate(_onSurfaceConfigure),
        ], out _xdgSurfaceListenerHandle);

        _xdgToplevel = WaylandNative.MarshalNew(
            _xdgSurface, Op.XdgSurfaceGetToplevel,
            XdgInterfaces.XdgToplevel, IntPtr.Zero);

        AddListener(_xdgToplevel, [
            Marshal.GetFunctionPointerForDelegate(_onToplevelConfigure),
            Marshal.GetFunctionPointerForDelegate(_onToplevelClose),
        ], out _toplevelListenerHandle);

        if (!desc.Borderless && _decorationManager != IntPtr.Zero)
        {
            _toplevelDecoration = WaylandNative.MarshalNewWithObj(
                _decorationManager, Op.DecorationManagerGetToplevel,
                DecorationInterfaces.ToplevelDecoration, 1u,
                IntPtr.Zero, _xdgToplevel);
            WaylandNative.SendArgs(_toplevelDecoration, Op.ToplevelDecorationSetMode,
                [WlArgument.Uint(Op.ToplevelDecorationModeServerSide)]);
        }

        SetTitle(desc.Title);
        WaylandNative.Send(_surface, Op.SurfaceCommit);
        WaylandNative.wl_display_roundtrip(_display);

        if (!_configureReceived)
            throw new InvalidOperationException(
                "No xdg_surface.configure received after initial commit.");

        int w = _pendingWidth  > 0 ? _pendingWidth  : desc.Width;
        int h = _pendingHeight > 0 ? _pendingHeight : desc.Height;
        CreateShmBuffer(w, h);

        WaylandNative.SendArgs(_surface, Op.SurfaceAttach,
            [WlArgument.Ptr(_buffer), WlArgument.Int(0), WlArgument.Int(0)]);
        WaylandNative.Send(_surface, Op.SurfaceCommit);
        WaylandNative.wl_display_flush(_display);
    }

    // =========================================================================
    // Shared-memory buffer
    // =========================================================================

    private unsafe void CreateShmBuffer(int width, int height)
    {
        _shmSize = width * height * 4;

        _shmFd = WaylandNative.memfd_create("humble-wl", WaylandNative.MFD_CLOEXEC);
        WaylandNative.ftruncate(_shmFd, _shmSize);

        _shmData = WaylandNative.mmap(IntPtr.Zero, new IntPtr(_shmSize),
            WaylandNative.PROT_READ_WRITE, WaylandNative.MAP_SHARED, _shmFd, 0);

        // Black placeholder, like the X11 background: reads as "nothing rendered
        // yet" where an arbitrary colour would read as a glitch.
        var pixels = new Span<uint>(_shmData.ToPointer(), width * height);
        pixels.Fill(0xFF000000);

        var pool = WaylandNative.MarshalNewWithFdInt(
            _wlShm, Op.ShmCreatePool, ShmPoolIface, 1, IntPtr.Zero, _shmFd, _shmSize);

        _buffer = WaylandNative.MarshalNewWith4IntUint(
            pool, Op.ShmPoolCreateBuffer, BufferIface, 1,
            IntPtr.Zero, 0, width, height, width * 4, 1 /* WL_SHM_FORMAT_XRGB8888 */);

        WaylandNative.SendDestroy(pool, Op.ShmPoolDestroy);
    }

    private void DestroyShmBuffer()
    {
        if (_buffer   != IntPtr.Zero) { WaylandNative.SendDestroy(_buffer, 0); _buffer = IntPtr.Zero; }
        if (_shmData  != IntPtr.Zero) { WaylandNative.munmap(_shmData, new IntPtr(_shmSize)); _shmData = IntPtr.Zero; }
        if (_shmFd    >= 0)           { WaylandNative.close(_shmFd); _shmFd = -1; }
    }

    // =========================================================================
    // Event loop
    // =========================================================================

    public override void PollEvents()
    {
        if (_useLibdecor)
        {
            // libdecor_dispatch handles flush + prepare_read + poll + dispatch_pending internally.
            LibDecorNative.libdecor_dispatch(_libdecorCtx, 0);
            PumpKeyRepeat();
            return;
        }

        WaylandNative.wl_display_flush(_display);

        var pfd = new WaylandNative.PollFd
        {
            fd     = WaylandNative.wl_display_get_fd(_display),
            events = WaylandNative.Pollin,
        };

        if (WaylandNative.wl_display_prepare_read(_display) == 0)
        {
            if (WaylandNative.Poll(ref pfd, 1, 0) > 0)
                WaylandNative.wl_display_read_events(_display);
            else
                WaylandNative.wl_display_cancel_read(_display);
        }

        WaylandNative.wl_display_dispatch_pending(_display);
        PumpKeyRepeat();
    }

    // =========================================================================
    // IWindow
    // =========================================================================

    public override void Show()
    {
        WaylandNative.Send(_surface, Op.SurfaceCommit);
        WaylandNative.wl_display_flush(_display);
    }

    public override void Hide() { }

    public override void SetTitle(string title)
    {
        if (_useLibdecor && _libdecorFrame != IntPtr.Zero)
        {
            LibDecorNative.libdecor_frame_set_title(_libdecorFrame, title);
            return;
        }
        var ptr = Marshal.StringToHGlobalAnsi(title);
        try
        {
            WaylandNative.SendArgs(_xdgToplevel, Op.XdgToplevelSetTitle,
                [WlArgument.Ptr(ptr)]);
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    public override void Resize(int width, int height) { }

    public override IWindow CreateChildWindow(WindowDescription description) =>
        new WaylandWindow(Backend, _display, description);

    // =========================================================================
    // INativeWindowHandle
    // =========================================================================

    /// <summary>Returns the <c>wl_surface*</c>.</summary>
    public IntPtr GetNativeHandle()     => _surface;

    /// <summary>Returns the <c>wl_display*</c>.</summary>
    public IntPtr GetConnectionHandle() => _display;

    /// <summary>
    /// Hands the surface content over to the renderer: destroys the shm
    /// placeholder buffer and stops re-attaching it on configure/commit.
    /// The renderer's swapchain attaches its own buffers from now on.
    /// </summary>
    public void NotifyRendererAttached()
    {
        _rendererOwnsSurface = true;
        DestroyShmBuffer();
    }

    // =========================================================================
    // Registry callbacks
    // =========================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlRegistryGlobal(IntPtr data, IntPtr registry, uint name, IntPtr iface, uint version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlRegistryGlobalRemove(IntPtr data, IntPtr registry, uint name);

    private void OnRegistryGlobal(IntPtr data, IntPtr registry, uint name, IntPtr ifacePtr, uint version)
    {
        var iface = Marshal.PtrToStringAnsi(ifacePtr) ?? string.Empty;

        if (iface == "wl_compositor")
        {
            _compositor = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, CompositorIface, Math.Min(version, 6u),
                name, Marshal.ReadIntPtr(CompositorIface, 0), Math.Min(version, 6u), IntPtr.Zero);
        }
        else if (iface == "wl_shm")
        {
            _wlShm = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, ShmIface, 1u,
                name, Marshal.ReadIntPtr(ShmIface, 0), 1u, IntPtr.Zero);
        }
        else if (iface == "xdg_wm_base" && !_useLibdecor)
        {
            _xdgWmBase = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, XdgInterfaces.XdgWmBase, 1u,
                name, XdgWmBaseName, 1u, IntPtr.Zero);
        }
        else if (iface == "zxdg_decoration_manager_v1" && !_useLibdecor)
        {
            _decorationManager = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, DecorationInterfaces.Manager, 1u,
                name, Marshal.ReadIntPtr(DecorationInterfaces.Manager, 0), 1u, IntPtr.Zero);
        }
        else if (iface == "wl_seat" && _seat == IntPtr.Zero)
        {
            // Version 1 on purpose: exactly the five v1 pointer events, no
            // frame batching, no discrete-axis variants — its listener below
            // matches that contract.
            _seat = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, SeatIface, 1u,
                name, Marshal.ReadIntPtr(SeatIface, 0), 1u, IntPtr.Zero);
            AddListener(_seat, [
                Marshal.GetFunctionPointerForDelegate(_onSeatCapabilities),
                Marshal.GetFunctionPointerForDelegate(_onSeatName),
            ], out _seatListenerHandle);
            TryCreateDataDevice();
        }
        else if (iface == "wl_data_device_manager")
        {
            _dataDeviceManager = WaylandNative.MarshalRegistryBind(
                registry, Op.RegistryBind, DataDeviceManagerIface, 1u,
                name, Marshal.ReadIntPtr(DataDeviceManagerIface, 0), 1u, IntPtr.Zero);
            TryCreateDataDevice();
        }
    }

    /// <summary>Creates the seat's data device once both the manager and the seat are bound (clipboard plumbing).</summary>
    private void TryCreateDataDevice()
    {
        if (_dataDevice != IntPtr.Zero || _dataDeviceManager == IntPtr.Zero || _seat == IntPtr.Zero)
            return;

        _dataDevice = WaylandNative.MarshalNewWithObj(
            _dataDeviceManager, Op.DataDeviceManagerGetDataDevice, DataDeviceIface, 1u, IntPtr.Zero, _seat);
        AddListener(_dataDevice, [
            Marshal.GetFunctionPointerForDelegate(_onDataOffer),
            Marshal.GetFunctionPointerForDelegate(_onDataEnter),
            Marshal.GetFunctionPointerForDelegate(_onDataLeave),
            Marshal.GetFunctionPointerForDelegate(_onDataMotion),
            Marshal.GetFunctionPointerForDelegate(_onDataDrop),
            Marshal.GetFunctionPointerForDelegate(_onDataSelection),
        ], out _dataDeviceListenerHandle);
    }

    private void OnRegistryGlobalRemove(IntPtr data, IntPtr registry, uint name) { }

    // =========================================================================
    // Input callbacks (wl_seat v1 + wl_pointer v1)
    // =========================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlSeatCapabilities(IntPtr data, IntPtr seat, uint capabilities);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlSeatName(IntPtr data, IntPtr seat, IntPtr name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlPointerEnter(IntPtr data, IntPtr pointer, uint serial, IntPtr surface, int surfaceX, int surfaceY);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlPointerLeave(IntPtr data, IntPtr pointer, uint serial, IntPtr surface);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlPointerMotion(IntPtr data, IntPtr pointer, uint time, int surfaceX, int surfaceY);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlPointerButton(IntPtr data, IntPtr pointer, uint serial, uint time, uint button, uint state);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlPointerAxis(IntPtr data, IntPtr pointer, uint time, uint axis, int value);

    /// <summary>Acquires wl_pointer and wl_keyboard once the seat declares the capabilities.</summary>
    private void OnSeatCapabilities(IntPtr data, IntPtr seat, uint capabilities)
    {
        const uint pointerCapability  = 1; // WL_SEAT_CAPABILITY_POINTER
        const uint keyboardCapability = 2; // WL_SEAT_CAPABILITY_KEYBOARD

        if ((capabilities & pointerCapability) != 0 && _pointer == IntPtr.Zero)
        {
            _pointer = WaylandNative.MarshalNew(_seat, Op.SeatGetPointer, PointerIface, IntPtr.Zero);
            AddListener(_pointer, [
                Marshal.GetFunctionPointerForDelegate(_onPointerEnter),
                Marshal.GetFunctionPointerForDelegate(_onPointerLeave),
                Marshal.GetFunctionPointerForDelegate(_onPointerMotion),
                Marshal.GetFunctionPointerForDelegate(_onPointerButton),
                Marshal.GetFunctionPointerForDelegate(_onPointerAxis),
            ], out _pointerListenerHandle);
        }

        if ((capabilities & keyboardCapability) != 0 && _keyboard == IntPtr.Zero)
        {
            _keyboard = WaylandNative.MarshalNew(_seat, Op.SeatGetKeyboard, KeyboardIface, IntPtr.Zero);
            AddListener(_keyboard, [
                Marshal.GetFunctionPointerForDelegate(_onKeyboardKeymap),
                Marshal.GetFunctionPointerForDelegate(_onKeyboardEnter),
                Marshal.GetFunctionPointerForDelegate(_onKeyboardLeave),
                Marshal.GetFunctionPointerForDelegate(_onKeyboardKey),
                Marshal.GetFunctionPointerForDelegate(_onKeyboardModifiers),
            ], out _keyboardListenerHandle);
        }
    }

    private void OnSeatName(IntPtr data, IntPtr seat, IntPtr name)
    {
    }

    private void OnPointerEnter(IntPtr data, IntPtr pointer, uint serial, IntPtr surface, int surfaceX, int surfaceY)
    {
        // The seat is shared: with libdecor, decoration surfaces produce enter/
        // leave too — only our content surface concerns the engine.
        if (surface != _surface)
            return;
        _pointerInside = true;
        _pointerX = WlFixedToDouble(surfaceX);
        _pointerY = WlFixedToDouble(surfaceY);
        RaiseInput(new MouseEntered(_mouse, PointerPosition()));
    }

    private void OnPointerLeave(IntPtr data, IntPtr pointer, uint serial, IntPtr surface)
    {
        if (surface != _surface || !_pointerInside)
            return;
        _pointerInside = false;
        RaiseInput(new MouseExited(_mouse));
    }

    private void OnPointerMotion(IntPtr data, IntPtr pointer, uint time, int surfaceX, int surfaceY)
    {
        if (!_pointerInside)
            return;
        _pointerX = WlFixedToDouble(surfaceX);
        _pointerY = WlFixedToDouble(surfaceY);
        RaiseInput(new MouseMoved(_mouse, PointerPosition()));
    }

    private void OnPointerButton(IntPtr data, IntPtr pointer, uint serial, uint time, uint button, uint state)
    {
        _lastSerial = serial; // set_selection needs a recent input serial
        if (!_pointerInside)
            return;

        // Linux input event codes: BTN_LEFT, BTN_RIGHT, BTN_MIDDLE.
        PointerButton? translated = button switch
        {
            0x110 => PointerButton.Left,
            0x111 => PointerButton.Right,
            0x112 => PointerButton.Middle,
            _     => null,
        };
        if (translated is null)
            return;

        RaiseInput(state == 1
            ? new MousePressed(_mouse, translated.Value, PointerPosition())
            : new MouseReleased(_mouse, translated.Value, PointerPosition()));
    }

    private void OnPointerAxis(IntPtr data, IntPtr pointer, uint time, uint axis, int value)
    {
        if (!_pointerInside)
            return;

        // Wayland axis values are continuous, ~15 units per wheel notch,
        // positive towards bottom/right; our convention is +Y up, +X right.
        var notches = (float)(WlFixedToDouble(value) / 15.0);
        var delta = axis == 0 /* vertical */
            ? new Vector2(0f, -notches)
            : new Vector2(notches, 0f);
        RaiseInput(new MouseScrolled(_mouse, delta, PointerPosition()));
    }

    /// <summary>wl_fixed_t is signed 24.8 fixed point.</summary>
    private static double WlFixedToDouble(int value) => value / 256.0;

    private Vector2 PointerPosition() => new((float)_pointerX, (float)_pointerY);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlKeyboardKeymap(IntPtr data, IntPtr keyboard, uint format, int fd, uint size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlKeyboardEnter(IntPtr data, IntPtr keyboard, uint serial, IntPtr surface, IntPtr keys);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlKeyboardLeave(IntPtr data, IntPtr keyboard, uint serial, IntPtr surface);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlKeyboardKey(IntPtr data, IntPtr keyboard, uint serial, uint time, uint key, uint state);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlKeyboardModifiers(IntPtr data, IntPtr keyboard, uint serial,
        uint depressedMods, uint latchedMods, uint lockedMods, uint group);

    /// <summary>
    /// Receives the compositor's keymap: mmap the fd, compile it with
    /// xkbcommon, build the state that will interpret every scancode.
    /// </summary>
    private void OnKeyboardKeymap(IntPtr data, IntPtr keyboard, uint format, int fd, uint size)
    {
        try
        {
            if (format != XkbNative.KeymapFormatTextV1)
                return;

            var mapped = WaylandNative.mmap(IntPtr.Zero, new IntPtr(size),
                WaylandNative.PROT_READ, WaylandNative.MAP_PRIVATE, fd, 0);
            if (mapped == new IntPtr(-1))
                return;

            try
            {
                DestroyXkb();
                _xkbContext = XkbNative.xkb_context_new(0);
                _xkbKeymap  = XkbNative.xkb_keymap_new_from_string(
                    _xkbContext, mapped, XkbNative.KeymapFormatTextV1, 0);
                if (_xkbKeymap != IntPtr.Zero)
                    _xkbState = XkbNative.xkb_state_new(_xkbKeymap);
            }
            finally
            {
                WaylandNative.munmap(mapped, new IntPtr(size));
            }
        }
        finally
        {
            WaylandNative.close(fd);
        }
    }

    private void OnKeyboardEnter(IntPtr data, IntPtr keyboard, uint serial, IntPtr surface, IntPtr keys)
    {
        if (surface == _surface)
            _keyboardInside = true;
    }

    private void OnKeyboardLeave(IntPtr data, IntPtr keyboard, uint serial, IntPtr surface)
    {
        if (surface != _surface)
            return;
        _keyboardInside = false;
        StopRepeat(); // a key held across a focus loss must not keep firing
    }

    /// <summary>
    /// A scancode arrived: xkbcommon turns it (+8, the historical X offset)
    /// into a keysym for the logical-key channel and UTF-8 for the text
    /// channel. Wayland does not repeat keys — client-side repeat is deferred
    /// to its client, the text field.
    /// </summary>
    private void OnKeyboardKey(IntPtr data, IntPtr keyboard, uint serial, uint time, uint key, uint state)
    {
        _lastSerial = serial; // a copy (Ctrl+C) sets the selection with this serial
        if (!_keyboardInside || _xkbState == IntPtr.Zero)
            return;

        var keycode   = key + 8;
        var keysym    = XkbNative.xkb_state_key_get_one_sym(_xkbState, keycode);
        var logical   = KeysymTranslation.ToKey(keysym);
        var modifiers = ReadModifiers();
        var pressed   = state == 1;

        if (!pressed)
        {
            RaiseInput(new KeyboardKeyReleased(_keyboardDevice, logical, modifiers));
            if (key == _repeatScancode)
                StopRepeat();
            return;
        }

        var keyEvent = new KeyboardKeyPressed(_keyboardDevice, logical, modifiers);
        RaiseInput(keyEvent);

        string? text = null;
        var count = XkbNative.xkb_state_key_get_utf8(_xkbState, keycode, _keyTextBuffer, (nuint)_keyTextBuffer.Length);
        if (count > 0)
        {
            var utf8 = System.Text.Encoding.UTF8.GetString(_keyTextBuffer, 0, count);
            if (utf8.Length > 0 && !char.IsControl(utf8[0]))
            {
                text = utf8;
                RaiseInput(new KeyboardTextInput(_keyboardDevice, text));
            }
        }

        // Arm the repeat on this key — the most recent press wins.
        _repeatScancode = key;
        _repeatKey      = keyEvent;
        _repeatText     = text;
        _repeating      = true;
        _lastRepeatMs   = 0;
        _repeatClock.Restart();
    }

    /// <summary>Stops any pending key repeat.</summary>
    private void StopRepeat()
    {
        _repeating = false;
        _repeatScancode = 0;
        _repeatKey = null;
        _repeatText = null;
    }

    /// <summary>
    /// Replays the held key after the delay, then every interval — called once per
    /// PollEvents, so the cadence is quantized to the frame (fine for ~30 Hz).
    /// </summary>
    private void PumpKeyRepeat()
    {
        if (!_repeating || !_keyboardInside || _repeatKey is null)
            return;

        var elapsed = _repeatClock.ElapsedMilliseconds;
        if (elapsed < RepeatDelayMs)
            return;
        if (_lastRepeatMs != 0 && elapsed - _lastRepeatMs < RepeatIntervalMs)
            return;

        _lastRepeatMs = elapsed;
        RaiseInput(_repeatKey);
        if (_repeatText is not null)
            RaiseInput(new KeyboardTextInput(_keyboardDevice, _repeatText));
    }

    private void OnKeyboardModifiers(IntPtr data, IntPtr keyboard, uint serial,
        uint depressedMods, uint latchedMods, uint lockedMods, uint group)
    {
        if (_xkbState != IntPtr.Zero)
            XkbNative.xkb_state_update_mask(_xkbState, depressedMods, latchedMods, lockedMods, 0, 0, group);
    }

    /// <summary>Effective modifiers read back from the xkb state, by their XKB names.</summary>
    private KeyModifiers ReadModifiers()
    {
        var modifiers = KeyModifiers.None;
        if (IsModifierActive("Shift"))   modifiers |= KeyModifiers.Shift;
        if (IsModifierActive("Control")) modifiers |= KeyModifiers.Ctrl;
        if (IsModifierActive("Mod1"))    modifiers |= KeyModifiers.Alt;
        if (IsModifierActive("Mod4"))    modifiers |= KeyModifiers.Super;
        return modifiers;
    }

    private bool IsModifierActive(string name) =>
        XkbNative.xkb_state_mod_name_is_active(_xkbState, name, XkbNative.StateModsEffective) == 1;

    /// <summary>Releases the xkbcommon objects, newest first.</summary>
    private void DestroyXkb()
    {
        if (_xkbState   != IntPtr.Zero) { XkbNative.xkb_state_unref(_xkbState);     _xkbState   = IntPtr.Zero; }
        if (_xkbKeymap  != IntPtr.Zero) { XkbNative.xkb_keymap_unref(_xkbKeymap);   _xkbKeymap  = IntPtr.Zero; }
        if (_xkbContext != IntPtr.Zero) { XkbNative.xkb_context_unref(_xkbContext); _xkbContext = IntPtr.Zero; }
    }

    // =========================================================================
    // Clipboard (wl_data_device / wl_data_source / wl_data_offer)
    // =========================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceDataOffer(IntPtr data, IntPtr device, IntPtr id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceEnter(IntPtr data, IntPtr device, uint serial, IntPtr surface, int x, int y, IntPtr id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceLeave(IntPtr data, IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceMotion(IntPtr data, IntPtr device, uint time, int x, int y);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceDrop(IntPtr data, IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataDeviceSelection(IntPtr data, IntPtr device, IntPtr id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataSourceTarget(IntPtr data, IntPtr source, IntPtr mime);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataSourceSend(IntPtr data, IntPtr source, IntPtr mime, int fd);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WlDataSourceCancelled(IntPtr data, IntPtr source);

    // A new offer is introduced before the selection event names it; we ask for
    // text/plain directly, so the offer's mime list is of no interest — only the
    // selection event's id matters. Drag-and-drop events are ignored.
    private void OnDataOffer(IntPtr data, IntPtr device, IntPtr id) { }
    private void OnDataEnter(IntPtr data, IntPtr device, uint serial, IntPtr surface, int x, int y, IntPtr id) { }
    private void OnDataLeave(IntPtr data, IntPtr device) { }
    private void OnDataMotion(IntPtr data, IntPtr device, uint time, int x, int y) { }
    private void OnDataDrop(IntPtr data, IntPtr device) { }

    /// <summary>The clipboard changed: keep the new offer for pasting, destroying the previous one.</summary>
    private void OnDataSelection(IntPtr data, IntPtr device, IntPtr id)
    {
        if (_currentOffer != IntPtr.Zero)
            WaylandNative.SendDestroy(_currentOffer, Op.DataOfferDestroy);
        _currentOffer = id; // null when the clipboard was cleared
    }

    private void OnSourceTarget(IntPtr data, IntPtr source, IntPtr mime) { }

    /// <summary>Another client (or us) is pasting our clipboard: write the text to the fd and close it.</summary>
    private void OnSourceSend(IntPtr data, IntPtr source, IntPtr mime, int fd)
    {
        if (_clipboardText is not null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(_clipboardText);
            WaylandNative.write(fd, bytes, bytes.Length);
        }
        WaylandNative.close(fd);
    }

    /// <summary>We lost the clipboard to another source — drop ours.</summary>
    private void OnSourceCancelled(IntPtr data, IntPtr source)
    {
        if (source == _dataSource)
        {
            WaylandNative.SendDestroy(_dataSource, Op.DataSourceDestroy);
            _dataSource = IntPtr.Zero;
            _clipboardText = null;
        }
    }

    /// <inheritdoc/>
    public override bool IsSuspended => _suspended;

    /// <inheritdoc/>
    public override IClipboard Clipboard => this;

    /// <summary>Offers <paramref name="text"/> as the clipboard selection (served on demand via the source's send event).</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _clipboardText = text;
        if (_dataDevice == IntPtr.Zero)
            return;

        if (_dataSource != IntPtr.Zero)
            WaylandNative.SendDestroy(_dataSource, Op.DataSourceDestroy);
        if (_dataSourceListenerHandle.IsAllocated)
            _dataSourceListenerHandle.Free();

        _dataSource = WaylandNative.MarshalNew(
            _dataDeviceManager, Op.DataDeviceManagerCreateDataSource, DataSourceIface, IntPtr.Zero);
        AddListener(_dataSource, [
            Marshal.GetFunctionPointerForDelegate(_onSourceTarget),
            Marshal.GetFunctionPointerForDelegate(_onSourceSend),
            Marshal.GetFunctionPointerForDelegate(_onSourceCancelled),
        ], out _dataSourceListenerHandle);

        var mime = Marshal.StringToHGlobalAnsi(ClipboardMime);
        try
        {
            WaylandNative.SendArgs(_dataSource, Op.DataSourceOffer, [WlArgument.Ptr(mime)]);
        }
        finally
        {
            Marshal.FreeHGlobal(mime);
        }

        WaylandNative.SendArgs(_dataDevice, Op.DataDeviceSetSelection,
            [WlArgument.Ptr(_dataSource), WlArgument.Uint(_lastSerial)]);
        WaylandNative.wl_display_flush(_display);
    }

    /// <summary>
    /// Reads the clipboard: ask the current offer to write text/plain into a pipe,
    /// pump the display (so our own source can answer), then read the pipe bounded.
    /// </summary>
    public string? GetText()
    {
        if (_currentOffer == IntPtr.Zero)
            return null;

        var fds = new int[2];
        if (WaylandNative.pipe2(fds, 0) != 0)
            return null;

        var mime = Marshal.StringToHGlobalAnsi(ClipboardMime);
        try
        {
            WaylandNative.SendArgs(_currentOffer, Op.DataOfferReceive,
                [WlArgument.Ptr(mime), WlArgument.Int(fds[1])]);
        }
        finally
        {
            Marshal.FreeHGlobal(mime);
        }

        WaylandNative.close(fds[1]); // we only read; the writer holds the other end
        WaylandNative.wl_display_flush(_display);
        WaylandNative.wl_display_roundtrip(_display); // let our own source answer, if we own it

        var collected = new List<byte>();
        var buffer = new byte[4096];
        var pfd = new WaylandNative.PollFd { fd = fds[0], events = WaylandNative.Pollin };
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 500)
        {
            if (WaylandNative.Poll(ref pfd, 1, 100) <= 0)
                break; // timeout or error — give up
            var read = WaylandNative.read(fds[0], buffer, buffer.Length);
            if (read <= 0)
                break; // EOF or error
            collected.AddRange(buffer[..(int)read]);
        }
        WaylandNative.close(fds[0]);

        return collected.Count == 0 ? null : System.Text.Encoding.UTF8.GetString(collected.ToArray());
    }

    // =========================================================================
    // Raw XDG Shell callbacks
    // =========================================================================

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void XdgWmBasePing(IntPtr data, IntPtr wmBase, uint serial);

    private void OnWmBasePing(IntPtr data, IntPtr wmBase, uint serial) =>
        WaylandNative.SendArgs(wmBase, Op.XdgWmBasePong, [WlArgument.Uint(serial)]);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void XdgSurfaceConfigure(IntPtr data, IntPtr xdgSurface, uint serial);

    private void OnXdgSurfaceConfigure(IntPtr data, IntPtr xdgSurface, uint serial)
    {
        _pendingSerial = serial;

        if (_pendingWidth > 0 && _pendingHeight > 0)
            RaiseResize(_pendingWidth, _pendingHeight);

        WaylandNative.SendArgs(xdgSurface, Op.XdgSurfaceAckConfigure, [WlArgument.Uint(serial)]);
        WaylandNative.Send(_surface, Op.SurfaceCommit);
        _configureReceived = true;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void XdgToplevelConfigure(IntPtr data, IntPtr toplevel, int width, int height, IntPtr states);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void XdgToplevelClose(IntPtr data, IntPtr toplevel);

    private void OnXdgToplevelConfigure(IntPtr data, IntPtr toplevel, int width, int height, IntPtr states)
    {
        if (width > 0 && height > 0)
        {
            _pendingWidth  = width;
            _pendingHeight = height;
        }
    }

    private void OnXdgToplevelClose(IntPtr data, IntPtr toplevel)
    {
        ShouldClose = true;
        RaiseClose();
    }

    // =========================================================================
    // libdecor callbacks
    // =========================================================================

    private void OnLibdecorError(IntPtr ctx, int error, IntPtr message)
    {
        var msg = Marshal.PtrToStringAnsi(message) ?? "unknown";
        throw new InvalidOperationException($"libdecor error {error}: {msg}");
    }

    private void OnLibdecorConfigure(IntPtr frame, IntPtr config, IntPtr userData)
    {
        bool hasSize = LibDecorNative.libdecor_configuration_get_content_size(
            config, frame, out int w, out int h);

        if (!hasSize || w <= 0 || h <= 0)
        {
            w = _pendingWidth  > 0 ? _pendingWidth  : _defaultWidth;
            h = _pendingHeight > 0 ? _pendingHeight : _defaultHeight;
        }

        bool sizeChanged = w != _pendingWidth || h != _pendingHeight;
        _pendingWidth  = w;
        _pendingHeight = h;

        // The compositor reports suspension here (libdecor ≥ 0.2 over xdg-shell v6);
        // older libdecor lacks the call — stay non-suspended then.
        try
        {
            if (LibDecorNative.libdecor_configuration_get_window_state(config, out var windowState))
                _suspended = (windowState & LibDecorNative.WindowStateSuspended) != 0;
        }
        catch (EntryPointNotFoundException)
        {
            // libdecor too old for window-state: leave _suspended as is.
        }

        if (!_rendererOwnsSurface)
        {
            // Always recreate the buffer (initial call or resize).
            DestroyShmBuffer();
            CreateShmBuffer(w, h);

            WaylandNative.SendArgs(_surface, Op.SurfaceAttach,
                [WlArgument.Ptr(_buffer), WlArgument.Int(0), WlArgument.Int(0)]);
        }

        // Acknowledge the configure by committing state back to libdecor.
        var state = LibDecorNative.libdecor_state_new(w, h);
        LibDecorNative.libdecor_frame_commit(frame, state, config);
        LibDecorNative.libdecor_state_free(state);

        WaylandNative.Send(_surface, Op.SurfaceCommit);

        if (_configureReceived && sizeChanged)
            RaiseResize(w, h);

        _configureReceived = true;
    }

    private void OnLibdecorClose(IntPtr frame, IntPtr userData)
    {
        ShouldClose = true;
        RaiseClose();
    }

    // libdecor fires commit when decoration plugin changes need a redraw (e.g. focus change).
    private void OnLibdecorCommit(IntPtr frame, IntPtr userData)
    {
        if (_rendererOwnsSurface || _buffer == IntPtr.Zero) return;
        WaylandNative.SendArgs(_surface, Op.SurfaceAttach,
            [WlArgument.Ptr(_buffer), WlArgument.Int(0), WlArgument.Int(0)]);
        WaylandNative.Send(_surface, Op.SurfaceCommit);
        WaylandNative.wl_display_flush(_display);
    }

    // =========================================================================
    // Disposal
    // =========================================================================

    private bool _disposed;

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_useLibdecor)
        {
            // libdecor_frame_unref destroys xdg_toplevel + xdg_surface internally;
            // we must NOT touch those objects ourselves.
            if (_libdecorFrame != IntPtr.Zero) { LibDecorNative.libdecor_frame_unref(_libdecorFrame); _libdecorFrame = IntPtr.Zero; }
            if (_libdecorCtx   != IntPtr.Zero) { LibDecorNative.libdecor_unref(_libdecorCtx);         _libdecorCtx   = IntPtr.Zero; }
        }
        else
        {
            if (_toplevelDecoration != IntPtr.Zero) { WaylandNative.SendDestroy(_toplevelDecoration, 0);                       _toplevelDecoration = IntPtr.Zero; }
            if (_xdgToplevel        != IntPtr.Zero) { WaylandNative.SendDestroy(_xdgToplevel, Op.XdgToplevelDestroy);          _xdgToplevel        = IntPtr.Zero; }
            if (_xdgSurface         != IntPtr.Zero) { WaylandNative.SendDestroy(_xdgSurface,  Op.XdgSurfaceDestroy);           _xdgSurface         = IntPtr.Zero; }
            if (_decorationManager  != IntPtr.Zero) { WaylandNative.SendDestroy(_decorationManager, 0);                        _decorationManager  = IntPtr.Zero; }
            if (_xdgWmBase          != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_xdgWmBase);                              _xdgWmBase          = IntPtr.Zero; }
        }

        DestroyShmBuffer();
        DestroyXkb();
        // Clipboard objects.
        if (_currentOffer       != IntPtr.Zero) { WaylandNative.SendDestroy(_currentOffer, Op.DataOfferDestroy);   _currentOffer       = IntPtr.Zero; }
        if (_dataSource         != IntPtr.Zero) { WaylandNative.SendDestroy(_dataSource, Op.DataSourceDestroy);    _dataSource         = IntPtr.Zero; }
        if (_dataDevice         != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_dataDevice);                     _dataDevice         = IntPtr.Zero; }
        if (_dataDeviceManager  != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_dataDeviceManager);              _dataDeviceManager  = IntPtr.Zero; }
        // Seat, pointer and keyboard were bound at version 1 (no destructor request): plain proxy destruction.
        if (_keyboard   != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_keyboard);              _keyboard   = IntPtr.Zero; }
        if (_pointer    != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_pointer);               _pointer    = IntPtr.Zero; }
        if (_seat       != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_seat);                  _seat       = IntPtr.Zero; }
        if (_surface    != IntPtr.Zero) { WaylandNative.SendDestroy(_surface, Op.SurfaceDestroy); _surface    = IntPtr.Zero; }
        if (_wlShm      != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_wlShm);                 _wlShm      = IntPtr.Zero; }
        if (_compositor != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_compositor);            _compositor = IntPtr.Zero; }
        if (_registry   != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_registry);              _registry   = IntPtr.Zero; }

        WaylandNative.wl_display_flush(_display);

        _registryListenerHandle.Free();
        if (_seatListenerHandle.IsAllocated)                _seatListenerHandle.Free();
        if (_pointerListenerHandle.IsAllocated)             _pointerListenerHandle.Free();
        if (_keyboardListenerHandle.IsAllocated)            _keyboardListenerHandle.Free();
        if (_wmBaseListenerHandle.IsAllocated)              _wmBaseListenerHandle.Free();
        if (_xdgSurfaceListenerHandle.IsAllocated)          _xdgSurfaceListenerHandle.Free();
        if (_toplevelListenerHandle.IsAllocated)            _toplevelListenerHandle.Free();
        if (_libdecorContextListenerHandle.IsAllocated)     _libdecorContextListenerHandle.Free();
        if (_libdecorFrameListenerHandle.IsAllocated)       _libdecorFrameListenerHandle.Free();
        if (_dataDeviceListenerHandle.IsAllocated)          _dataDeviceListenerHandle.Free();
        if (_dataSourceListenerHandle.IsAllocated)          _dataSourceListenerHandle.Free();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static void AddListener(IntPtr proxy, IntPtr[] vtable, out GCHandle handle)
    {
        handle = GCHandle.Alloc(vtable, GCHandleType.Pinned);
        WaylandNative.wl_proxy_add_listener(proxy, handle.AddrOfPinnedObject(), IntPtr.Zero);
    }
}
