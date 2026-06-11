namespace HumbleEngine;

public interface IRenderer : IDisposable
{
    void BeginFrame();
    void EndFrame();
    void Present();
}
