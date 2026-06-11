namespace HumbleEngine.OpenGL;

/// <summary>OpenGL graphics backend — stub, not yet implemented.</summary>
public sealed class OpenGLGraphicsBackend : IGraphicsBackend
{
    /// <inheritdoc/>
    public string Name => "OpenGL";

    /// <inheritdoc/>
    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    /// <inheritdoc/>
    public void Initialize() =>
        throw new NotImplementedException("Backend OpenGL pas encore implémenté.");

    /// <inheritdoc/>
    public IRenderer CreateRenderer(IGraphicsSurface surface) =>
        throw new NotImplementedException("Backend OpenGL pas encore implémenté.");

    /// <inheritdoc/>
    public void Dispose() { }
}
