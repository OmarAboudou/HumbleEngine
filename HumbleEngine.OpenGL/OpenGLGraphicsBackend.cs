namespace HumbleEngine.OpenGL;

public sealed class OpenGLGraphicsBackend : IGraphicsBackend
{
    public string Name => "OpenGL";

    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    public void Initialize() =>
        throw new NotImplementedException("Backend OpenGL pas encore implémenté.");

    public IRenderer CreateRenderer(IGraphicsSurface surface) =>
        throw new NotImplementedException("Backend OpenGL pas encore implémenté.");

    public void Dispose() { }
}
