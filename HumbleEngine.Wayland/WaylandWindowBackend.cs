namespace HumbleEngine.Wayland;

// Wayland requiert des bindings générés (protocole brut wayland-client ou NWayland).
// Implémentation prévue dans un prochain bloc de la Phase 5.
public sealed class WaylandWindowBackend : IWindowBackend
{
    public string Name => "Wayland";

    public void Initialize() =>
        throw new NotImplementedException(
            "Backend Wayland pas encore implémenté. Utilisez X11WindowBackend.");

    public IWindow CreateWindow(WindowDescription description) =>
        throw new NotImplementedException("Backend Wayland pas encore implémenté.");

    public void Dispose() { }
}
