namespace HumbleEngine;

/// <summary>
/// Common base for all renderable surfaces (windows and mobile surfaces).
/// Represents a render context tied to a display area.
/// </summary>
public interface IGraphicsSurface : IDisposable
{
    /// <summary>The platform has signalled that this surface should close.</summary>
    bool ShouldClose { get; }

    /// <summary>Fired when the surface is closed by the user or the OS.</summary>
    event Action? OnClose;

    /// <summary>
    /// Runs the main loop: calls <paramref name="onFrame"/> each iteration
    /// until <see cref="ShouldClose"/> becomes true.
    /// </summary>
    void Run(Action onFrame);
}
