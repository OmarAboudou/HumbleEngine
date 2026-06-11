namespace HumbleEngine.Wayland;

/// <summary>
/// Wayland windowing backend — stub, not yet implemented.
/// Requires generated bindings (raw wayland-client protocol or NWayland).
/// Implementation planned for a later block in Phase 5.
/// </summary>
public sealed class WaylandWindowBackend : IWindowBackend
{
    /// <inheritdoc/>
    public string Name => "Wayland";

    /// <inheritdoc/>
    public void Initialize() =>
        throw new NotImplementedException(
            "Backend Wayland pas encore implémenté. Utilisez X11WindowBackend.");

    /// <inheritdoc/>
    public IWindow CreateWindow(WindowDescription description) =>
        throw new NotImplementedException("Backend Wayland pas encore implémenté.");

    /// <inheritdoc/>
    public void Dispose() { }
}
