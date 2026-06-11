namespace HumbleEngine;

public interface IGraphicSurfaceBackend : IDisposable
{
    public string Name { get; }
    public void Initialize();
}