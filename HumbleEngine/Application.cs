using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;

namespace HumbleEngine;

public sealed class Application : IDisposable
{
    private readonly IWindow _window;
    private GRContext?   _grContext;
    private SKSurface?   _surface;

    public Node? Root { get; set; }

    public Application(string title = "HumbleEngine", int width = 800, int height = 600)
    {
        var options = WindowOptions.Default;
        options.Title  = title;
        options.Size   = new Vector2D<int>(width, height);
        options.PreferredStencilBufferBits = 8;

        _window = Window.Create(options);
        _window.Load    += OnLoad;
        _window.Update  += OnUpdate;
        _window.Render  += OnRender;
        _window.Resize  += OnResize;
        _window.Closing += OnClosing;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        var glInterface = GRGlInterface.Create(name =>
        {
            _window.GLContext!.TryGetProcAddress(name, out var addr);
            return addr;
        });

        _grContext = GRContext.CreateGl(glInterface);
        CreateSurface();

        if (Root is null) return;
        Root.Init();
        Root.MarkLayoutDirty();
    }

    private void CreateSurface()
    {
        _surface?.Dispose();

        var (w, h)     = (_window.Size.X, _window.Size.Y);
        var fbInfo     = new GRGlFramebufferInfo(0, 0x8058); // GL_RGBA8
        var renderTarget = new GRBackendRenderTarget(w, h, 0, 8, fbInfo);

        _surface = SKSurface.Create(
            _grContext,
            renderTarget,
            GRSurfaceOrigin.BottomLeft,
            SKColorType.Rgba8888);
    }

    private void OnUpdate(double delta) => Root?.Update((float)delta);

    private void OnRender(double delta)
    {
        if (Root is null || _surface is null) return;

        var canvas = _surface.Canvas;

        Root.Layout(new Size(_window.Size.X, _window.Size.Y));
        canvas.Clear(SKColors.White);
        Root.Paint(canvas);
        canvas.Flush();

        Root.ClearDirty();
    }

    private void OnResize(Vector2D<int> _)
    {
        CreateSurface();
        Root?.MarkLayoutDirty();
    }

    private void OnClosing() => Root?.Dispose();

    public void Dispose()
    {
        _surface?.Dispose();
        _grContext?.Dispose();
        _window.Dispose();
    }
}
