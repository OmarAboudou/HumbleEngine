namespace HumbleEngine;

/// <summary>
/// Base implementation for desktop windows using the Template Method pattern.
/// <see cref="Run"/> drives the main loop; subclasses implement <see cref="PollEvents"/>
/// to process their native event queue.
/// </summary>
public abstract class Window : IWindow
{
    /// <inheritdoc/>
    public bool ShouldClose { get; protected set; }

    /// <inheritdoc/>
    public event Action? OnClose;

    /// <inheritdoc/>
    public event Action<int, int>? OnResize;

    /// <inheritdoc/>
    public void Run(Action onFrame)
    {
        while (!ShouldClose)
        {
            PollEvents();
            onFrame();
        }
    }

    /// <summary>
    /// Drains the backend's native event queue for one iteration.
    /// Must run on the main thread (required by X11, Win32, and Cocoa).
    /// </summary>
    protected abstract void PollEvents();

    /// <inheritdoc/>
    public abstract void Show();

    /// <inheritdoc/>
    public abstract void Hide();

    /// <inheritdoc/>
    public abstract void SetTitle(string title);

    /// <inheritdoc/>
    public abstract void Resize(int width, int height);

    /// <inheritdoc/>
    public abstract IWindow CreateChildWindow(WindowDescription description);

    /// <inheritdoc/>
    public abstract void Dispose();

    /// <summary>Raises <see cref="OnClose"/>.</summary>
    protected void RaiseClose() => OnClose?.Invoke();

    /// <summary>Raises <see cref="OnResize"/> with the new dimensions in pixels.</summary>
    protected void RaiseResize(int width, int height) => OnResize?.Invoke(width, height);
}
