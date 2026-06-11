namespace HumbleEngine;

public interface IGraphicsBackend : IDisposable
{
    public string Name { get; }
    public void Initialize();
}