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
internal sealed class WaylandWindow : Window, INativeWindowHandle
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

    private GCHandle _registryListenerHandle;
    private GCHandle _wmBaseListenerHandle;
    private GCHandle _xdgSurfaceListenerHandle;
    private GCHandle _toplevelListenerHandle;

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

        var pixels = new Span<uint>(_shmData.ToPointer(), width * height);
        pixels.Fill(0xFF1E3A5F);

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

    protected override void PollEvents()
    {
        if (_useLibdecor)
        {
            // libdecor_dispatch handles flush + prepare_read + poll + dispatch_pending internally.
            LibDecorNative.libdecor_dispatch(_libdecorCtx, 0);
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
    }

    private void OnRegistryGlobalRemove(IntPtr data, IntPtr registry, uint name) { }

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
        if (_surface    != IntPtr.Zero) { WaylandNative.SendDestroy(_surface, Op.SurfaceDestroy); _surface    = IntPtr.Zero; }
        if (_wlShm      != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_wlShm);                 _wlShm      = IntPtr.Zero; }
        if (_compositor != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_compositor);            _compositor = IntPtr.Zero; }
        if (_registry   != IntPtr.Zero) { WaylandNative.wl_proxy_destroy(_registry);              _registry   = IntPtr.Zero; }

        WaylandNative.wl_display_flush(_display);

        _registryListenerHandle.Free();
        if (_wmBaseListenerHandle.IsAllocated)              _wmBaseListenerHandle.Free();
        if (_xdgSurfaceListenerHandle.IsAllocated)          _xdgSurfaceListenerHandle.Free();
        if (_toplevelListenerHandle.IsAllocated)            _toplevelListenerHandle.Free();
        if (_libdecorContextListenerHandle.IsAllocated)     _libdecorContextListenerHandle.Free();
        if (_libdecorFrameListenerHandle.IsAllocated)       _libdecorFrameListenerHandle.Free();
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
