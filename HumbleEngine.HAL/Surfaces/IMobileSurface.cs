namespace HumbleEngine;

/// <summary>
/// Mobile surface — lifecycle driven by the OS (pause, resume, memory pressure).
/// </summary>
public interface IMobileSurface : IGraphicsSurface
{
    /// <summary>The application is moving to the background.</summary>
    event Action? OnPause;

    /// <summary>The application is returning to the foreground.</summary>
    event Action? OnResume;

    /// <summary>The OS is signalling memory pressure; release non-critical resources.</summary>
    event Action? OnLowMemory;
}
