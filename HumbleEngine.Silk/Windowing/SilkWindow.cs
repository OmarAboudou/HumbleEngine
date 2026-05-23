using HumbleEngine;
using SilkRawImage  = Silk.NET.Core.RawImage;
using SilkVec2      = Silk.NET.Maths.Vector2D<int>;
using SilkWin       = Silk.NET.Windowing.IWindow;
using SilkWinBorder = Silk.NET.Windowing.WindowBorder;
using SilkWinState  = Silk.NET.Windowing.WindowState;

namespace HumbleEngine.Silk;

public class SilkWindow : SilkViewport, IWindow
{
    private readonly SilkWin _window;

    private readonly Signal<Vector2<int>> _onMove         = new();
    private readonly Signal<WindowState>  _onStateChanged = new();
    private readonly Signal<string[]>     _onFileDrop     = new();

    public SilkWindow(SilkWin window) : base(window)
    {
        _window = window;
        _window.Move         += pos   => _onMove.Emit(new Vector2<int>(pos.X, pos.Y));
        _window.StateChanged += state => _onStateChanged.Emit(ToWindowState(state));
        _window.FileDrop     += files => _onFileDrop.Emit(files);
    }

    public string       Title        { get => _window.Title;       set => _window.Title       = value; }
    public Vector2<int> Position     { get => new(_window.Position.X, _window.Position.Y); set => _window.Position = new(value.X, value.Y); }
    public WindowState  WindowState  { get => ToWindowState(_window.WindowState);           set => _window.WindowState  = ToSilkState(value); }
    public WindowBorder WindowBorder { get => ToWindowBorder(_window.WindowBorder);         set => _window.WindowBorder = ToSilkBorder(value); }
    public bool         IsVisible    { get => _window.IsVisible;  set => _window.IsVisible  = value; }
    public bool         TopMost      { get => _window.TopMost;    set => _window.TopMost    = value; }
    public IWindow?  Parent     => _window.Parent is SilkWin w ? new SilkWindow(w) : null;
    public Insets BorderSize
    {
        get
        {
            var b = _window.BorderSize;
            return new Insets(b.Origin.X, b.Origin.Y, b.Size.X, b.Size.Y);
        }
    }
    public IMonitor? Monitor
    {
        get
        {
            var m = _window.Monitor;
            if (m is null) return null;
            var main = global::Silk.NET.Windowing.Monitor.GetMainMonitor(_window);
            return new SilkMonitor(m, main?.Index == m.Index);
        }
    }

    public IReadOnlySignal<Vector2<int>> OnMove         => _onMove.AsReadOnly();
    public IReadOnlySignal<WindowState>  OnStateChanged => _onStateChanged.AsReadOnly();
    public IReadOnlySignal<string[]>     OnFileDrop     => _onFileDrop.AsReadOnly();

    public IWindow CreateChildWindow(WindowOptions options)
    {
        var opts = global::Silk.NET.Windowing.WindowOptions.Default with
        {
            Title        = options.Title,
            Size         = new(options.Size.X, options.Size.Y),
            Position     = options.Position is { } p ? new(p.X, p.Y) : new(-1, -1),
            WindowState  = ToSilkState(options.WindowState),
            WindowBorder = ToSilkBorder(options.WindowBorder),
            IsVisible    = options.IsVisible,
            TopMost      = options.TopMost,
        };
        return new SilkWindow(_window.CreateWindow(opts));
    }

    public void SetWindowIcon(ReadOnlySpan<RawImage> icons)
    {
        var silkIcons = new SilkRawImage[icons.Length];
        for (var i = 0; i < icons.Length; i++)
            silkIcons[i] = new SilkRawImage(icons[i].Width, icons[i].Height, icons[i].Pixels);
        _window.SetWindowIcon(silkIcons);
    }

    private static WindowState ToWindowState(SilkWinState s) => s switch
    {
        SilkWinState.Minimized  => WindowState.Minimized,
        SilkWinState.Maximized  => WindowState.Maximized,
        SilkWinState.Fullscreen => WindowState.Fullscreen,
        _                       => WindowState.Normal,
    };

    private static SilkWinState ToSilkState(WindowState s) => s switch
    {
        WindowState.Minimized  => SilkWinState.Minimized,
        WindowState.Maximized  => SilkWinState.Maximized,
        WindowState.Fullscreen => SilkWinState.Fullscreen,
        _                      => SilkWinState.Normal,
    };

    private static WindowBorder ToWindowBorder(SilkWinBorder b) => b switch
    {
        SilkWinBorder.Fixed  => WindowBorder.Fixed,
        SilkWinBorder.Hidden => WindowBorder.Hidden,
        _                    => WindowBorder.Resizable,
    };

    private static SilkWinBorder ToSilkBorder(WindowBorder b) => b switch
    {
        WindowBorder.Fixed  => SilkWinBorder.Fixed,
        WindowBorder.Hidden => SilkWinBorder.Hidden,
        _                   => SilkWinBorder.Resizable,
    };
}
