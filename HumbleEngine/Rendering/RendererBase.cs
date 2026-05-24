namespace HumbleEngine;

public abstract class RendererBase : IRenderer
{
    protected IViewport? Viewport { get; private set; }

    private readonly Signal<ICanvas> _onBeginFrame = new();
    private readonly Signal          _onEndFrame   = new();
    public IReadOnlySignal<ICanvas> OnBeginFrame => _onBeginFrame.AsReadOnly();
    public IReadOnlySignal          OnEndFrame   => _onEndFrame.AsReadOnly();

    public void Attach(IViewport viewport)
    {
        Viewport = viewport;
        viewport.OnFramebufferResize.Connect(OnFramebufferResized);
        viewport.OnRender.Connect(OnRendered);

        if (viewport.IsInitialized)
            Initialize();
        else
            viewport.OnLoad.Connect(Initialize);
    }

    public void Detach()
    {
        if (Viewport is null) return;
        Viewport.OnLoad.Disconnect(Initialize);
        Viewport.OnFramebufferResize.Disconnect(OnFramebufferResized);
        Viewport.OnRender.Disconnect(OnRendered);
        Viewport = null;
        DestroyGraphics();
    }

    protected abstract void    Initialize();
    protected abstract void    CreateSurface();
    protected abstract void    DestroySurface();
    protected abstract ICanvas? GetCanvas();
    protected abstract void    Flush();

    protected virtual void DestroyGraphics() => DestroySurface();

    public abstract IPaint  CreatePaint();
    public abstract IShader CreateLinearGradient(Vector2<float> start, Vector2<float> end,   Color[] colors, float[]? positions = null);
    public abstract IShader CreateRadialGradient(Vector2<float> center, float radius, Color[] colors, float[]? positions = null);

    private void OnFramebufferResized(Vector2<int> _)
    {
        DestroySurface();
        CreateSurface();
    }

    private void OnRendered(double _)
    {
        var canvas = GetCanvas();
        if (canvas is null) return;
        _onBeginFrame.Emit(canvas);
        Flush();
        _onEndFrame.Emit();
    }
}
