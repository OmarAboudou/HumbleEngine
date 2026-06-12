namespace HumbleEngine;

/// <summary>
/// Graphics backend: initialises a rendering API (Vulkan, OpenGL…)
/// and creates renderers bound to a surface.
/// </summary>
public interface IGraphicsBackend : IDisposable
{
    /// <summary>Human-readable backend name, e.g. "Vulkan" or "OpenGL".</summary>
    string Name { get; }

    /// <summary>
    /// Window backend types that are compatible with this graphics backend.
    /// </summary>
    IReadOnlyList<Type> CompatibleWindowBackends { get; }

    /// <summary>
    /// Returns whether this graphics backend is compatible with the given window backend.
    /// The default implementation checks whether the backend's type is in <see cref="CompatibleWindowBackends"/>.
    /// Override for additional runtime checks (e.g. required Vulkan extensions).
    /// </summary>
    bool Supports(IWindowBackend backend) =>
        CompatibleWindowBackends.Contains(backend.GetType());

    /// <summary>
    /// Initialises the graphics API (creates the instance, selects a device…). Idempotent.
    /// </summary>
    void Initialize();

    /// <summary>Creates a renderer bound to the given surface.</summary>
    IRenderer CreateRenderer(IGraphicsSurface surface);
}
