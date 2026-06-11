namespace HumbleEngine;

// Implémentée par les fenêtres concrètes pour exposer leur handle natif
// aux backends graphiques (XID, HWND, wl_surface*, NSWindow*...).
// Invisible à la couche applicative — usage réservé aux backends HAL.
public interface INativeWindowHandle
{
    IntPtr GetNativeHandle();
}
