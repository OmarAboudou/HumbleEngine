namespace HumbleEngine;

/// <summary>
/// Base implementation for desktop windows using the Template Method pattern.
/// <see cref="Run"/> drives the main loop; subclasses implement <see cref="PollEvents"/>
/// to process their native event queue.
/// </summary>
public abstract class Window : IWindow
{
    /// <summary>Binds the window to the backend that creates it.</summary>
    protected Window(IWindowBackend backend) => Backend = backend;

    /// <inheritdoc/>
    public IWindowBackend Backend { get; }

    /// <inheritdoc/>
    public bool ShouldClose { get; protected set; }

    /// <inheritdoc/>
    public int Width { get; protected set; }

    /// <inheritdoc/>
    public int Height { get; protected set; }

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

    /// <summary>
    /// Updates <see cref="Width"/>/<see cref="Height"/> and raises <see cref="OnResize"/>
    /// with the new dimensions in pixels.
    /// </summary>
    protected void RaiseResize(int width, int height)
    {
        Width  = width;
        Height = height;
        OnResize?.Invoke(width, height);
    }
}
