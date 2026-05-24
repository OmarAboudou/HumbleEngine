using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaRenderer : IRenderer
{
    private IViewport?  _viewport;
    private GRContext?  _grContext;
    private SKSurface?  _surface;
    private SkiaCanvas? _canvas;

    private readonly Signal<ICanvas> _onBeginFrame = new();
    private readonly Signal         _onEndFrame   = new();
    public IReadOnlySignal<ICanvas> OnBeginFrame => _onBeginFrame.AsReadOnly();
    public IReadOnlySignal         OnEndFrame   => _onEndFrame.AsReadOnly();

    public void Attach(IViewport viewport)
    {
        _viewport = viewport;
        _viewport.OnFramebufferResize.Connect(OnFramebufferResize);
        _viewport.OnRender.Connect(OnRender);

        if (viewport.IsInitialized)
            OnLoad();
        else
            _viewport.OnLoad.Connect(OnLoad);
    }

    public void Detach()
    {
        if (_viewport is null) return;
        _viewport.OnLoad.Disconnect(OnLoad);
        _viewport.OnFramebufferResize.Disconnect(OnFramebufferResize);
        _viewport.OnRender.Disconnect(OnRender);
        _viewport = null;
        DestroySurface();
        _grContext?.Dispose();
        _grContext = null;
    }

    private void OnLoad()
    {
        var ctx         = _viewport!.GraphicsContext!;
        var glInterface = GRGlInterface.Create(ctx.GetProcAddress);
        _grContext      = GRContext.CreateGl(glInterface);
        CreateSurface();
    }

    private void OnFramebufferResize(Vector2<int> _)
    {
        DestroySurface();
        CreateSurface();
    }

    private void OnRender(double _)
    {
        if (_surface is null || _canvas is null) return;

        _onBeginFrame.Emit(_canvas);
        _surface.Canvas.Flush();
        _onEndFrame.Emit();
    }

    private void CreateSurface()
    {
        if (_grContext is null || _viewport is null) return;

        var size = _viewport.FramebufferSize;

        var renderTarget = new GRBackendRenderTarget(
            size.X, size.Y,
            sampleCount: 0,
            stencilBits: 8,
            new GRGlFramebufferInfo(fboId: 0, format: 0x8058) // GL_RGBA8
        );

        _surface?.Dispose();
        _surface = SKSurface.Create(
            _grContext,
            renderTarget,
            GRSurfaceOrigin.BottomLeft,
            SKColorType.Rgba8888
        );
        _canvas = new SkiaCanvas(_surface.Canvas);
    }

    private void DestroySurface()
    {
        _surface?.Dispose();
        _surface = null;
        _canvas  = null;
    }

    public IPaint CreatePaint() => new SkiaPaint();

    public IFont CreateFont(ITypeface? typeface, float size)
    {
        var skia = typeface as SkiaTypeface ?? new SkiaTypeface(SKTypeface.Default);
        return new SkiaFont(skia, size);
    }

    public IShader CreateLinearGradient(Vector2<float> start, Vector2<float> end, Color[] colors, float[]? positions = null)
        => SkiaShader.CreateLinearGradient(start, end, colors, positions);

    public IShader CreateRadialGradient(Vector2<float> center, float radius, Color[] colors, float[]? positions = null)
        => SkiaShader.CreateRadialGradient(center, radius, colors, positions);
}
