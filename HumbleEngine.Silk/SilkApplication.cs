using HumbleEngine;
using Silk.NET.Windowing;
using SilkVec2      = Silk.NET.Maths.Vector2D<int>;
using SilkWinOpts   = Silk.NET.Windowing.WindowOptions;
using SilkWinState  = Silk.NET.Windowing.WindowState;
using SilkWinBorder = Silk.NET.Windowing.WindowBorder;

namespace HumbleEngine.Silk;

public class SilkApplication : Application<WindowNode>
{
    protected IRenderer? Renderer { get; private set; }

    protected virtual IRenderer? CreateRenderer() => null;
    protected virtual void OnRendererCreated(IRenderer renderer)
    {
        ConnectRenderPasses(renderer);
    }

    protected override WindowNode CreateRootNode(ApplicationConfig config)
    {
        var opts = SilkWinOpts.Default with
        {
            Title        = config.WindowOptions.Title,
            Size         = new SilkVec2(config.WindowOptions.Size.X, config.WindowOptions.Size.Y),
            Position     = config.WindowOptions.Position is { } p ? new SilkVec2(p.X, p.Y) : new SilkVec2(-1, -1),
            WindowState  = ToSilkState(config.WindowOptions.WindowState),
            WindowBorder = ToSilkBorder(config.WindowOptions.WindowBorder),
            IsVisible    = config.WindowOptions.IsVisible,
            TopMost      = config.WindowOptions.TopMost,
            API          = GraphicsAPI.Default,
        };

        var silkWin    = global::Silk.NET.Windowing.Window.Create(opts);
        var silkWindow = new SilkWindow(silkWin);
        var root       = new WindowNode(silkWindow);

        root.Viewport.OnLoad.Connect(() =>
        {
            var renderer = CreateRenderer();
            if (renderer is null) return;

            Renderer = renderer;
            renderer.Attach(root.Viewport);
            OnRendererCreated(renderer);
        });

        root.Viewport.OnClosing.Connect(() =>
        {
            Renderer?.Detach();
            Renderer = null;
        });

        return root;
    }

    private static SilkWinState ToSilkState(WindowState s) => s switch
    {
        WindowState.Minimized  => SilkWinState.Minimized,
        WindowState.Maximized  => SilkWinState.Maximized,
        WindowState.Fullscreen => SilkWinState.Fullscreen,
        _                      => SilkWinState.Normal,
    };

    private static SilkWinBorder ToSilkBorder(WindowBorder b) => b switch
    {
        WindowBorder.Fixed  => SilkWinBorder.Fixed,
        WindowBorder.Hidden => SilkWinBorder.Hidden,
        _                   => SilkWinBorder.Resizable,
    };
}
