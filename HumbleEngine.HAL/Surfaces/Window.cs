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
    /// <remarks>Backends with a suspend signal (Wayland, X11) override this; the default is never suspended.</remarks>
    public virtual bool IsSuspended => false;

    /// <inheritdoc/>
    /// <remarks>
    /// Always pumps events; renders only when not suspended — a minimised or fully
    /// occluded window would otherwise block on a present that never completes,
    /// freezing the whole loop (and with it every other window's input and clipboard).
    /// </remarks>
    public bool Step(Action onFrame)
    {
        if (ShouldClose)
            return false;
        PollEvents();
        if (ShouldClose)
            return false;
        if (!IsSuspended)
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
    /// <remarks>Backends with clipboard support override this; the default refuses.</remarks>
    public virtual IClipboard Clipboard =>
        throw new NotSupportedException("This window backend has no clipboard support.");

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
