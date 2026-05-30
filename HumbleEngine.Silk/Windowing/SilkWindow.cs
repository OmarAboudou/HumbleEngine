using HumbleEngine;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SlkWindow = Silk.NET.Windowing.Window;

namespace HumbleEngine.Silk;

public class SilkWindow : PlatformWindow
{
    private readonly IWindow window;

    public SilkWindow(int width, int height)
    {
        window = SlkWindow.Create(
            WindowOptions.Default with
            {
                Title            = Title.Value,
                Size             = new Vector2D<int>(width, height),
                UpdatesPerSecond = 60
            }
        );
        window.Load              += EmitLoaded;
        window.Update            += EmitFixUpdated;
        window.Render            += EmitRendering;
        window.Closing           += EmitClosing;
        window.FramebufferResize += size => EmitResized(new Size(size.X, size.Y));
    }

    public override void Run()   => window.Run();
    public override void Reset() => window.Reset();

    protected override void SetTitle(string title) => window.Title = title;

    public override void Dispose()
    {
        base.Dispose();
        window.Dispose();
    }
}
