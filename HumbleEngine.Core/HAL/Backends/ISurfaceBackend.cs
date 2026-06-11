namespace HumbleEngine;

/// <summary>Common contract for all surface backends (windowing and mobile).</summary>
public interface ISurfaceBackend : IDisposable
{
    /// <summary>Human-readable backend name, e.g. "X11" or "Wayland".</summary>
    string Name { get; }

    /// <summary>
    /// Initialises the backend's system resources. Idempotent: no-op if already called.
    /// </summary>
    void Initialize();
}
