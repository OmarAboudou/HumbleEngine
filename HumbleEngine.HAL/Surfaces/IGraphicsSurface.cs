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
    /// Single input channel: every platform event, translated by the backend
    /// into the HAL vocabulary (<see cref="InputEvent"/> hierarchy), in
    /// arrival order, raised while the surface pumps (main thread). One
    /// channel because routing wants one pipe — the application typically
    /// wires it straight to its tree: <c>surface.OnInput += tree.RouteInput</c>.
    /// </summary>
    event Action<InputEvent>? OnInput;

    /// <summary>
    /// Advances this surface by one loop iteration: processes pending platform
    /// events, then runs <paramref name="onFrame"/> — skipped when the platform
    /// asked to close meanwhile. Returns <c>true</c> while the surface wants to
    /// continue (the <c>IEnumerator.MoveNext</c> idiom), so a multi-window loop
    /// is one <c>while</c> condition: <c>while (a.Step(fa) &amp;&amp; b.Step(fb))</c>.
    /// Each surface family implements its own iteration (desktop pumps its
    /// queue; a platform whose OS owns the loop overrides <see cref="Run"/> instead).
    /// </summary>
    bool Step(Action onFrame);

    /// <summary>
    /// Runs the main loop until the platform asks to close — literally
    /// <c>while (Step(onFrame))</c>: the single-surface convenience, composed
    /// once here at the contract level.
    /// </summary>
    void Run(Action onFrame)
    {
        while (Step(onFrame))
        {
        }
    }
}
