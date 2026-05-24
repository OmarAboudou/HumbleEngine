using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaRenderer : RendererBase
{
    private GRContext?   _grContext;
    private SKSurface?   _surface;
    private SkiaCanvas?  _canvas;

    protected override void Initialize()
    {
        // GRGlInterface.Create() resolves GL functions via the platform's native mechanism
        // (glXGetProcAddress on Linux). Passing a custom delegate crashes in SkiaSharp 3.x.
        var glInterface = GRGlInterface.Create();
        _grContext      = GRContext.CreateGl(glInterface);
        CreateSurface();
    }

    protected override void CreateSurface()
    {
        if (_grContext is null || Viewport is null) return;

        var size = Viewport.FramebufferSize;
        var renderTarget = new GRBackendRenderTarget(
            size.X, size.Y,
            sampleCount: 0,
            stencilBits: 8,
            new GRGlFramebufferInfo(fboId: 0, format: 0x8058) // GL_RGBA8
        );

        _surface?.Dispose();
        _surface = SKSurface.Create(_grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        _canvas  = new SkiaCanvas(_surface.Canvas);
    }

    protected override void DestroySurface()
    {
        _surface?.Dispose();
        _surface = null;
        _canvas  = null;
    }

    protected override void     DestroyGraphics() { base.DestroyGraphics(); _grContext?.Dispose(); _grContext = null; }
    protected override ICanvas? GetCanvas()       => _canvas;
    protected override void     Flush()           => _surface?.Canvas.Flush();

    public override IPaint CreatePaint() => new SkiaPaint();

    public override IShader CreateLinearGradient(Vector2<float> start, Vector2<float> end, Color[] colors, float[]? positions = null)
        => SkiaShader.CreateLinearGradient(start, end, colors, positions);

    public override IShader CreateRadialGradient(Vector2<float> center, float radius, Color[] colors, float[]? positions = null)
        => SkiaShader.CreateRadialGradient(center, radius, colors, positions);
}
