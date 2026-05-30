using System.Numerics;
using System.Runtime.InteropServices;
using HumbleEngine;
using SkiaSharp;

namespace HumbleEngine.Skia;

public sealed class SkiaRenderer : IRenderer
{
    private GRContext?  _grContext;
    private SKSurface?  _surface;
    private Size        _size;

    public bool Supports(GPUBackend backend) =>
        backend is GPUBackend.OpenGL or GPUBackend.Vulkan or GPUBackend.Metal or GPUBackend.Software;

    public void Initialize(GPUBackend backend)
    {
        _grContext = backend switch
        {
            GPUBackend.OpenGL   => GRContext.CreateGl(CreateGlInterface()),
            GPUBackend.Software => null,
            _ => throw new NotSupportedException($"Backend {backend} not yet implemented.")
        };
        RecreateSurface();
    }

    private static GRGlInterface CreateGlInterface()
    {
        IntPtr lib = IntPtr.Zero;
        NativeLibrary.TryLoad("libGL.so.1",   out lib);
        if (lib == IntPtr.Zero) NativeLibrary.TryLoad("libGL.so",     out lib);
        if (lib == IntPtr.Zero) NativeLibrary.TryLoad("opengl32.dll", out lib);
        if (lib == IntPtr.Zero) NativeLibrary.TryLoad("libGL.dylib",  out lib);

        return GRGlInterface.CreateOpenGl(name =>
        {
            if (lib != IntPtr.Zero && NativeLibrary.TryGetExport(lib, name, out IntPtr ptr))
                return ptr;
            return IntPtr.Zero;
        });
    }

    public void Resize(Size size)
    {
        _size = size;
        RecreateSurface();
    }

    public void Render(PaintCommandBuffer buffer)
    {
        if (_surface is null) return;

        SKCanvas canvas = _surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        foreach (PaintCommand command in buffer.Commands)
            Execute(canvas, command);

        canvas.Flush();
    }

    private void Execute(SKCanvas canvas, PaintCommand command)
    {
        switch (command)
        {
            case FillRect(var bounds, var color):
                using (var paint = new SKPaint { Color = ToSkColor(color), IsAntialias = true })
                    canvas.DrawRect(ToSkRect(bounds), paint);
                break;

            case FillRRect(var bounds, var radius, var color):
                using (var paint = new SKPaint { Color = ToSkColor(color), IsAntialias = true })
                    canvas.DrawRoundRect(ToSkRoundRect(bounds, radius), paint);
                break;

            case FillOval(var bounds, var color):
                using (var paint = new SKPaint { Color = ToSkColor(color), IsAntialias = true })
                    canvas.DrawOval(ToSkRect(bounds), paint);
                break;

            case StrokeRRect(var bounds, var radius, var color, var thickness):
                using (var paint = new SKPaint { Color = ToSkColor(color), IsStroke = true, StrokeWidth = thickness, IsAntialias = true })
                    canvas.DrawRoundRect(ToSkRoundRect(bounds, radius), paint);
                break;

            case DrawShadow(var bounds, var radius, var color, var blurRadius, var spreadRadius, var dx, var dy):
                var shadowBounds = bounds.Inflate(spreadRadius * 2f, spreadRadius * 2f).Translate(dx, dy);
                using (var paint = new SKPaint
                {
                    Color       = ToSkColor(color),
                    MaskFilter  = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blurRadius / 3f),
                    IsAntialias = true
                })
                    canvas.DrawRoundRect(ToSkRoundRect(shadowBounds, radius), paint);
                break;

            case PushClipRect(var bounds):
                canvas.Save();
                canvas.ClipRect(ToSkRect(bounds), SKClipOperation.Intersect, true);
                break;

            case PushClipRRect(var bounds, var radius):
                canvas.Save();
                canvas.ClipRoundRect(ToSkRoundRect(bounds, radius), SKClipOperation.Intersect, true);
                break;

            case PushClipOval(var bounds):
                canvas.Save();
                using (var path = new SKPath())
                {
                    path.AddOval(ToSkRect(bounds));
                    canvas.ClipPath(path, SKClipOperation.Intersect, true);
                }
                break;

            case PushOpacity(var alpha):
                using (var layerPaint = new SKPaint { Color = new SKColor(0, 0, 0, alpha) })
                    canvas.SaveLayer(layerPaint);
                break;

            case PushTransform(var matrix):
                canvas.Save();
                canvas.SetMatrix(canvas.TotalMatrix.PreConcat(ToSkMatrix(matrix)));
                break;

            case Pop _:
                canvas.Restore();
                break;
        }
    }

    private void RecreateSurface()
    {
        _surface?.Dispose();
        _surface = null;

        if (_size.Width <= 0 || _size.Height <= 0) return;

        if (_grContext is not null)
        {
            var fbInfo       = new GRGlFramebufferInfo(0, 0x8058); // GL_RGBA8
            var renderTarget = new GRBackendRenderTarget((int)_size.Width, (int)_size.Height, 0, 8, fbInfo);
            _surface = SKSurface.Create(_grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        }
        else
        {
            _surface = SKSurface.Create(new SKImageInfo((int)_size.Width, (int)_size.Height));
        }
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _grContext?.Dispose();
    }

    private static SKColor     ToSkColor(Color c)          => new(c.R, c.G, c.B, c.A);
    private static SKRect      ToSkRect(Rect r)             => new(r.X, r.Y, r.Right, r.Bottom);
    private static SKRoundRect ToSkRoundRect(Rect r, BorderRadius br)
    {
        var rr = new SKRoundRect();
        rr.SetRectRadii(ToSkRect(r),
        [
            new SKPoint(br.TopLeft,     br.TopLeft),
            new SKPoint(br.TopRight,    br.TopRight),
            new SKPoint(br.BottomRight, br.BottomRight),
            new SKPoint(br.BottomLeft,  br.BottomLeft),
        ]);
        return rr;
    }
    private static SKMatrix    ToSkMatrix(Matrix3x2 m)      =>
        new(m.M11, m.M21, m.M31,
            m.M12, m.M22, m.M32,
            0,     0,     1);
}
