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
            "Wayland backend not yet implemented. Use X11WindowBackend instead.");

    /// <inheritdoc/>
    public IWindow CreateWindow(WindowDescription description) =>
        throw new NotImplementedException("Wayland backend not yet implemented.");

    /// <inheritdoc/>
    public void Dispose() { }
}
