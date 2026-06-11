namespace HumbleEngine;

public interface IGraphicsSurface : IDisposable
{
    bool ShouldClose { get; }
    event Action? OnClose;
    void Run(Action onFrame);
}
