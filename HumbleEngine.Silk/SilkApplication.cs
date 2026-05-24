using HumbleEngine;
using Silk.NET.Windowing;
using SilkVec2      = Silk.NET.Maths.Vector2D<int>;
using SilkWinOpts   = Silk.NET.Windowing.WindowOptions;
using SilkWinState  = Silk.NET.Windowing.WindowState;
using SilkWinBorder = Silk.NET.Windowing.WindowBorder;

namespace HumbleEngine.Silk;

public abstract class SilkApplication : Application<WindowNode>
{
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
            API          = global::Silk.NET.Windowing.GraphicsAPI.Default,
        };

        var silkWin = Window.Create(opts);
        return new WindowNode(new SilkWindow(silkWin));
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
