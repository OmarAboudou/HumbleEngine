using HumbleEngine;

namespace HumbleEngine.OpenGL;

internal sealed class OpenGLRenderer : IRenderer
{
    public void BeginFrame() => throw new NotImplementedException("Renderer OpenGL pas encore implémenté.");
    public void EndFrame()   => throw new NotImplementedException();
    public void Present()    => throw new NotImplementedException();
    public void Dispose()    { }
}
