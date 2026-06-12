namespace HumbleEngine;

/// <summary>
/// Base implementation for desktop windows. <see cref="PollEvents"/> is the
/// pump primitive each backend implements; <see cref="Step"/> composes one
/// loop iteration from it (pump, then frame); <c>Run</c> comes from the
/// <see cref="IGraphicsSurface"/> contract. Applications with several windows
/// write their own loop over <see cref="Step"/> — the loop policy (exit
/// condition, frame order) is theirs, like everything else in the engine.
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
    public event Action<InputEvent>? OnInput;

    /// <inheritdoc/>
    public bool Step(Action onFrame)
    {
        if (ShouldClose)
            return false;
        PollEvents();
        if (ShouldClose)
            return false;
        onFrame();
        return !ShouldClose;
    }

    /// <inheritdoc/>
    public abstract void PollEvents();

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

    /// <summary>Publishes a translated input event on the single channel.</summary>
    protected void RaiseInput(InputEvent inputEvent) => OnInput?.Invoke(inputEvent);

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
