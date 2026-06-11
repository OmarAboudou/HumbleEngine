namespace HumbleEngine;

/// <summary>
/// Exposes native handles to graphics backends.
/// Not visible to the application layer — for HAL backends only.
/// </summary>
public interface INativeWindowHandle
{
    /// <summary>
    /// Returns the platform-specific window handle.
    /// XID on X11, HWND on Win32, wl_surface* on Wayland, NSWindow* on macOS.
    /// </summary>
    IntPtr GetNativeHandle();

    /// <summary>
    /// Returns the platform-specific connection or display handle,
    /// or <see cref="IntPtr.Zero"/> on platforms where the window handle is self-contained.
    /// Display* on X11, wl_display* on Wayland, null on Win32/macOS.
    /// </summary>
    IntPtr GetConnectionHandle();

    /// <summary>
    /// Notifies the window that a renderer now owns the surface content.
    /// Windows that attach a placeholder buffer of their own (e.g. the Wayland
    /// shm buffer) must stop doing so to avoid fighting the renderer's
    /// presentation. Default: no-op.
    /// </summary>
    void NotifyRendererAttached() { }
}
