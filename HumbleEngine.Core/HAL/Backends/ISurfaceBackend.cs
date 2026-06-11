namespace HumbleEngine;

public interface ISurfaceBackend : IDisposable
{
    string Name { get; }
    // Initialize est idempotent : sans effet si déjà appelé.
    void Initialize();
}
