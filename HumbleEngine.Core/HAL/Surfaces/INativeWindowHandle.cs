namespace HumbleEngine;

/// <summary>
/// Exposes the native window handle to graphics backends.
/// Not visible to the application layer — for HAL backends only.
/// </summary>
/// <remarks>
/// The returned value is platform-specific:
/// XID on X11, HWND on Win32, wl_surface* on Wayland, NSWindow* on macOS.
/// </remarks>
public interface INativeWindowHandle
{
    /// <summary>Returns the platform-specific native window handle.</summary>
    IntPtr GetNativeHandle();
}
