using System.Runtime.InteropServices;

namespace HumbleEngine.Wayland;

/// <summary>
/// Wayland argument union — 8 bytes on 64-bit Linux, matching <c>union wl_argument</c>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct WlArgument
{
    [FieldOffset(0)] public int    i;
    [FieldOffset(0)] public uint   u;
    [FieldOffset(0)] public long   raw; // use for const char* and wl_object*

    internal static WlArgument Uint(uint v)   => new() { u   = v };
    internal static WlArgument Int(int v)    => new() { i   = v };
    internal static WlArgument Ptr(IntPtr v)  => new() { raw = v.ToInt64() };
}

/// <summary>
/// <c>struct wl_interface</c> layout — 64-bit Linux, matches the C ABI exactly.
/// All fields are required: <c>wl_proxy_marshal_*</c> reads <c>methods[opcode].signature</c>
/// to know argument types before writing to the wire.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WlInterface
{
    public IntPtr Name;        // const char*              (+0)
    public int    Version;     //                          (+8)
    public int    MethodCount; //                          (+12)
    public IntPtr Methods;     // const struct wl_message* (+16)
    public int    EventCount;  //                          (+24)
    private int   _pad;        //                          (+28)
    public IntPtr Events;      // const struct wl_message* (+32)
}

/// <summary>
/// <c>struct wl_message</c> layout — 24 bytes on 64-bit Linux.
/// <c>Types</c> is an array of <c>wl_interface*</c>, one per 'o'/'n' argument;
/// setting it to NULL skips interface validation (safe for our use).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WlMessage
{
    public IntPtr Name;       // const char* (+0)
    public IntPtr Signature;  // const char* (+8) — argument type string
    public IntPtr Types;      // const struct wl_interface** (+16) — NULL = no validation
}

/// <summary>
/// XDG Shell interface descriptors with full method/event tables, allocated in unmanaged memory.
/// <para>
/// The built-in wayland interfaces (wl_compositor, wl_registry, …) live in libwayland-client.so.
/// XDG shell has no such symbols — we define the tables manually.
/// </para>
/// <para>
/// Signature characters: u=uint32, i=int32, s=string, o=object, n=new_id, a=wl_array,
/// ?=nullable prefix, 1-9=since-version prefix (ignored at runtime).
/// </para>
/// </summary>
internal static class XdgInterfaces
{
    internal static readonly IntPtr XdgWmBase;
    internal static readonly IntPtr XdgSurface;
    internal static readonly IntPtr XdgToplevel;

    static XdgInterfaces()
    {
        // xdg_wm_base — we call opcodes 2 (get_xdg_surface) and 3 (pong)
        var wmMethods = Msgs(
            ("destroy",           ""),      // 0
            ("create_positioner", "n"),     // 1
            ("get_xdg_surface",   "no"),    // 2
            ("pong",              "u"));    // 3
        var wmEvents = Msgs(
            ("ping", "u"));                 // 0
        XdgWmBase = Iface("xdg_wm_base", 6, wmMethods, 4, wmEvents, 1);

        // xdg_surface — we call opcodes 0 (destroy), 1 (get_toplevel), 4 (ack_configure)
        var sfcMethods = Msgs(
            ("destroy",             ""),        // 0
            ("get_toplevel",        "n"),       // 1
            ("get_popup",           "n?oo"),    // 2
            ("set_window_geometry", "iiii"),    // 3
            ("ack_configure",       "u"));      // 4
        var sfcEvents = Msgs(
            ("configure", "u"));                // 0
        XdgSurface = Iface("xdg_surface", 6, sfcMethods, 5, sfcEvents, 1);

        // xdg_toplevel — we call opcodes 0 (destroy) and 2 (set_title)
        // Binding at version 1 caps events to configure(0) and close(1).
        var tlMethods = Msgs(
            ("destroy",    ""),     // 0
            ("set_parent", "?o"),   // 1
            ("set_title",  "s"));   // 2
        var tlEvents = Msgs(
            ("configure", "iia"),   // 0
            ("close",     ""));     // 1
        XdgToplevel = Iface("xdg_toplevel", 1, tlMethods, 3, tlEvents, 2);
    }

    // Allocates a contiguous array of WlMessage structs in unmanaged memory.
    internal static IntPtr Msgs(params (string name, string sig)[] entries)
    {
        int stride = Marshal.SizeOf<WlMessage>();
        var ptr = Marshal.AllocHGlobal(entries.Length * stride);
        for (int i = 0; i < entries.Length; i++)
        {
            Marshal.StructureToPtr(new WlMessage {
                Name      = Marshal.StringToHGlobalAnsi(entries[i].name),
                Signature = Marshal.StringToHGlobalAnsi(entries[i].sig),
                Types     = IntPtr.Zero,
            }, IntPtr.Add(ptr, i * stride), false);
        }
        return ptr;
    }

    // Allocates a WlInterface struct in unmanaged memory.
    internal static IntPtr Iface(string name, int version,
        IntPtr methods, int methodCount, IntPtr events, int eventCount)
    {
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<WlInterface>());
        Marshal.StructureToPtr(new WlInterface {
            Name        = Marshal.StringToHGlobalAnsi(name),
            Version     = version,
            MethodCount = methodCount,
            Methods     = methods,
            EventCount  = eventCount,
            Events      = events,
        }, ptr, false);
        return ptr;
    }
}

/// <summary>
/// Interface descriptors for the XDG Decoration unstable protocol
/// (<c>zxdg_decoration_manager_v1</c>), which lets clients request server-side decorations.
/// Not in libwayland-client.so — defined manually like the XDG shell interfaces.
/// </summary>
internal static class DecorationInterfaces
{
    internal static readonly IntPtr Manager;
    internal static readonly IntPtr ToplevelDecoration;

    static DecorationInterfaces()
    {
        var mgr = XdgInterfaces.Msgs(
            ("destroy",                  ""),    // 0
            ("get_toplevel_decoration",  "no")); // 1
        Manager = XdgInterfaces.Iface("zxdg_decoration_manager_v1", 1, mgr, 2, IntPtr.Zero, 0);

        var decMethods = XdgInterfaces.Msgs(
            ("destroy",    ""),   // 0
            ("set_mode",   "u"),  // 1
            ("unset_mode", ""));  // 2
        var decEvents = XdgInterfaces.Msgs(
            ("configure", "u"));  // 0
        ToplevelDecoration = XdgInterfaces.Iface("zxdg_toplevel_decoration_v1", 1, decMethods, 3, decEvents, 1);
    }
}

/// <summary>Protocol opcodes for Wayland core and XDG Shell objects.</summary>
internal static class Op
{
    // wl_display
    internal const uint DisplayGetRegistry = 1;
    // wl_registry
    internal const uint RegistryBind = 0;
    // wl_compositor
    internal const uint CompositorCreateSurface = 0;
    // wl_surface
    internal const uint SurfaceDestroy = 0;
    internal const uint SurfaceAttach  = 1;
    internal const uint SurfaceCommit  = 6;
    // wl_shm
    internal const uint ShmCreatePool = 0;
    // wl_shm_pool
    internal const uint ShmPoolCreateBuffer = 0;
    internal const uint ShmPoolDestroy      = 1;
    // xdg_wm_base
    internal const uint XdgWmBaseGetXdgSurface = 2;
    internal const uint XdgWmBasePong          = 3;
    // xdg_surface
    internal const uint XdgSurfaceDestroy      = 0;
    internal const uint XdgSurfaceGetToplevel  = 1;
    internal const uint XdgSurfaceAckConfigure = 4;
    // xdg_toplevel
    internal const uint XdgToplevelDestroy  = 0;
    internal const uint XdgToplevelSetTitle = 2;
    // wl_seat
    internal const uint SeatGetPointer  = 0;
    internal const uint SeatGetKeyboard = 1;
    // wl_data_device_manager
    internal const uint DataDeviceManagerCreateDataSource = 0;
    internal const uint DataDeviceManagerGetDataDevice    = 1;
    // wl_data_device
    internal const uint DataDeviceSetSelection = 1;
    internal const uint DataDeviceRelease      = 2;
    // wl_data_source
    internal const uint DataSourceOffer   = 0;
    internal const uint DataSourceDestroy  = 1;
    // wl_data_offer
    internal const uint DataOfferReceive = 1;
    internal const uint DataOfferDestroy = 2;
    // zxdg_decoration_manager_v1
    internal const uint DecorationManagerGetToplevel = 1;
    // zxdg_toplevel_decoration_v1
    internal const uint ToplevelDecorationSetMode = 1;
    internal const uint ToplevelDecorationModeServerSide = 2;
}

internal static class WaylandNative
{
    private const string Lib = "libwayland-client.so.0";

    // --- Connection ---
    [DllImport(Lib)] internal static extern IntPtr wl_display_connect(string? name);
    [DllImport(Lib)] internal static extern void   wl_display_disconnect(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_flush(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_dispatch_pending(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_roundtrip(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_get_fd(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_prepare_read(IntPtr display);
    [DllImport(Lib)] internal static extern void   wl_display_cancel_read(IntPtr display);
    [DllImport(Lib)] internal static extern int    wl_display_read_events(IntPtr display);

    // --- Proxy ---
    [DllImport(Lib)] internal static extern void wl_proxy_destroy(IntPtr proxy);
    [DllImport(Lib)] internal static extern int  wl_proxy_add_listener(IntPtr proxy, IntPtr impl, IntPtr data);

    // --- Marshal: create object with no extra args (e.g. get_registry, get_toplevel, create_surface) ---
    [DllImport(Lib, EntryPoint = "wl_proxy_marshal_constructor", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr MarshalNew(IntPtr proxy, uint opcode, IntPtr iface, IntPtr nullArg);

    // --- Marshal: create object with one object arg (e.g. xdg_wm_base.get_xdg_surface(surface)) ---
    [DllImport(Lib, EntryPoint = "wl_proxy_marshal_constructor_versioned", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr MarshalNewWithObj(
        IntPtr proxy, uint opcode, IntPtr iface, uint version, IntPtr nullArg, IntPtr objArg);

    // --- Marshal: registry.bind (name, iface_name, version, new_id_placeholder) ---
    [DllImport(Lib, EntryPoint = "wl_proxy_marshal_constructor_versioned", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr MarshalRegistryBind(
        IntPtr registry, uint opcode, IntPtr iface, uint version,
        uint   name,
        IntPtr ifaceName,  // const char*
        uint   version2,
        IntPtr newIdNull);

    // --- Marshal: send request, no new object ---
    [DllImport(Lib)]
    private static extern IntPtr wl_proxy_marshal_array_flags(
        IntPtr proxy, uint opcode, IntPtr iface, uint version, uint flags, IntPtr args);

    // No-arg request (commit, destroy, etc.)
    internal static void Send(IntPtr proxy, uint opcode) =>
        wl_proxy_marshal_array_flags(proxy, opcode, IntPtr.Zero, 0, 0, IntPtr.Zero);

    // Destroy request (sets WL_MARSHAL_FLAG_DESTROY=1, no separate wl_proxy_destroy needed)
    internal static void SendDestroy(IntPtr proxy, uint destroyOpcode) =>
        wl_proxy_marshal_array_flags(proxy, destroyOpcode, IntPtr.Zero, 0, 1, IntPtr.Zero);

    // Request with arguments
    internal static unsafe void SendArgs(IntPtr proxy, uint opcode, ReadOnlySpan<WlArgument> args)
    {
        fixed (WlArgument* p = args)
            wl_proxy_marshal_array_flags(proxy, opcode, IntPtr.Zero, 0, 0, new IntPtr(p));
    }

    // --- poll(2) for non-blocking event reads ---
    [DllImport("libc", EntryPoint = "poll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Poll(ref PollFd fd, uint nfds, int timeoutMs);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFd { public int fd; public short events; public short revents; }
    internal const short Pollin = 0x001;

    // --- Marshal: wl_shm.create_pool(id, fd, size) — signature "nhi" ---
    [DllImport(Lib, EntryPoint = "wl_proxy_marshal_constructor_versioned", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr MarshalNewWithFdInt(
        IntPtr proxy, uint opcode, IntPtr iface, uint version,
        IntPtr nullArg, int fd, int size);

    // --- Marshal: wl_shm_pool.create_buffer(id, offset, width, height, stride, format) — signature "niiiiu" ---
    [DllImport(Lib, EntryPoint = "wl_proxy_marshal_constructor_versioned", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr MarshalNewWith4IntUint(
        IntPtr proxy, uint opcode, IntPtr iface, uint version,
        IntPtr nullArg, int offset, int width, int height, int stride, uint format);

    // --- Shared memory via libc ---
    // Creates an anonymous file in memory, suitable for passing to wl_shm.
    [DllImport("libc", EntryPoint = "memfd_create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int memfd_create([MarshalAs(UnmanagedType.LPStr)] string name, uint flags);
    internal const uint MFD_CLOEXEC = 1;

    [DllImport("libc", EntryPoint = "ftruncate", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int ftruncate(int fd, long length);

    [DllImport("libc", EntryPoint = "mmap", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, long offset);
    internal const int PROT_READ_WRITE = 3; // PROT_READ | PROT_WRITE
    internal const int PROT_READ       = 1;
    internal const int MAP_SHARED      = 1;
    internal const int MAP_PRIVATE     = 2;

    [DllImport("libc", EntryPoint = "munmap", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int munmap(IntPtr addr, IntPtr length);

    [DllImport("libc", EntryPoint = "close", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int close(int fd);

    // --- Pipe for clipboard data transfer (the source writes, the requestor reads) ---
    [DllImport("libc", EntryPoint = "pipe2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int pipe2(int[] fds, int flags);

    [DllImport("libc", EntryPoint = "read", CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint read(int fd, byte[] buffer, nint count);

    [DllImport("libc", EntryPoint = "write", CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint write(int fd, byte[] buffer, nint count);

    // --- Get interface pointers for built-in Wayland objects ---
    internal static IntPtr GetBuiltinInterface(string symbolName)
    {
        var lib = System.Runtime.InteropServices.NativeLibrary.Load(Lib);
        return System.Runtime.InteropServices.NativeLibrary.GetExport(lib, symbolName);
    }
}

/// <summary>
/// P/Invoke bindings for <c>libdecor-0.so.0</c> — client-side decoration library
/// used by SDL2 and Firefox on GNOME Wayland.
/// <para>
/// GNOME/Mutter deliberately does not expose <c>zxdg_decoration_manager_v1</c>,
/// so server-side decorations are unavailable there. <c>libdecor</c> draws CSD
/// (title bar, buttons) via a plugin, transparently across compositors.
/// </para>
/// </summary>
internal static class LibDecorNative
{
    private const string Lib = "libdecor-0.so.0";

    /// <summary><c>true</c> when libdecor is present on the system.</summary>
    internal static readonly bool IsAvailable;

    static LibDecorNative()
    {
        try
        {
            System.Runtime.InteropServices.NativeLibrary.Load(Lib);
            IsAvailable = true;
        }
        catch { IsAvailable = false; }
    }

    // --- Context (one per wl_display) ---

    /// <summary>
    /// Creates a libdecor context.
    /// <c>iface</c> is a pinned <c>libdecor_interface</c> vtable:
    /// 11 function pointers (error callback + 10 reserved nulls).
    /// </summary>
    [DllImport(Lib)] internal static extern IntPtr libdecor_new(IntPtr display, IntPtr iface);

    /// <summary>Decrements the context ref-count; frees when it reaches zero.</summary>
    [DllImport(Lib)] internal static extern void libdecor_unref(IntPtr context);

    /// <summary>
    /// Pumps the Wayland event loop through libdecor.
    /// Pass <c>timeout = 0</c> for non-blocking, <c>-1</c> to block until an event arrives.
    /// </summary>
    [DllImport(Lib)] internal static extern int libdecor_dispatch(IntPtr context, int timeout);

    // --- Frame (one per window surface) ---

    /// <summary>
    /// Attaches libdecor decorations to a <c>wl_surface</c>.
    /// libdecor takes over <c>xdg_surface</c>/<c>xdg_toplevel</c> management internally.
    /// <c>frameIface</c> is a pinned <c>libdecor_frame_interface</c> vtable:
    /// 13 function pointers (configure, close, commit, dismiss_popup + 9 reserved nulls).
    /// </summary>
    [DllImport(Lib)] internal static extern IntPtr libdecor_decorate(IntPtr context, IntPtr surface, IntPtr frameIface, IntPtr userData);

    /// <summary>Decrements the frame ref-count.</summary>
    [DllImport(Lib)] internal static extern void libdecor_frame_unref(IntPtr frame);

    /// <summary>Sets the window title shown in the decoration bar.</summary>
    [DllImport(Lib, CharSet = CharSet.Ansi)] internal static extern void libdecor_frame_set_title(IntPtr frame, string title);

    /// <summary>Maps (shows) the decorated window.</summary>
    [DllImport(Lib)] internal static extern void libdecor_frame_map(IntPtr frame);

    // --- State / configuration ---

    /// <summary>Allocates a <c>libdecor_state</c> describing the current content size.</summary>
    [DllImport(Lib)] internal static extern IntPtr libdecor_state_new(int width, int height);

    /// <summary>Frees a <c>libdecor_state</c>.</summary>
    [DllImport(Lib)] internal static extern void libdecor_state_free(IntPtr state);

    /// <summary>
    /// Acknowledges a configure event and commits the new state.
    /// Must be called from the <c>configure</c> frame callback; <c>configuration</c>
    /// is the pointer received in that callback.
    /// </summary>
    [DllImport(Lib)] internal static extern void libdecor_frame_commit(IntPtr frame, IntPtr state, IntPtr configuration);

    /// <summary>
    /// Reads the compositor-requested content size out of a configuration.
    /// Returns <c>true</c> when the compositor specified a size; <c>false</c> means
    /// "keep your current size".
    /// </summary>
    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool libdecor_configuration_get_content_size(
        IntPtr configuration, IntPtr frame, out int width, out int height);

    /// <summary>
    /// Reads the window-state bitmask of a configuration. <c>SUSPENDED</c> (1 &lt;&lt; 7,
    /// libdecor ≥ 0.2 over xdg-shell v6) means the compositor is not showing the
    /// window — returns <c>false</c> on libdecor versions that lack the call.
    /// </summary>
    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool libdecor_configuration_get_window_state(
        IntPtr configuration, out uint windowState);

    /// <summary><c>LIBDECOR_WINDOW_STATE_SUSPENDED</c> — the surface is not visible.</summary>
    internal const uint WindowStateSuspended = 1u << 7;
}
