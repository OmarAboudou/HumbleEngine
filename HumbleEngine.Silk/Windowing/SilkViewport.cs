using HumbleEngine;
using Silk.NET.Input;
using SilkVec2 = Silk.NET.Maths.Vector2D<int>;
using SilkView = Silk.NET.Windowing.IView;

namespace HumbleEngine.Silk;

public class SilkViewport : IViewport
{
    protected readonly SilkView _view;
    private SilkInputContext? _input;

    private readonly Signal               _onLoad               = new();
    private readonly Signal<double>       _onUpdate             = new();
    private readonly Signal<double>       _onRender             = new();
    private readonly Signal<Vector2<int>> _onResized            = new();
    private readonly Signal<Vector2<int>> _onFramebufferResize  = new();
    private readonly Signal<bool>         _onFocusChanged       = new();
    private readonly Signal               _onClosing            = new();

    public SilkViewport(SilkView view)
    {
        _view = view;
        _view.Load              += ()      => _onLoad.Emit();
        _view.Update            += delta   => _onUpdate.Emit(delta);
        _view.Render            += delta   => _onRender.Emit(delta);
        _view.Resize            += size    => _onResized.Emit(new Vector2<int>(size.X, size.Y));
        _view.FramebufferResize += size    => _onFramebufferResize.Emit(new Vector2<int>(size.X, size.Y));
        _view.FocusChanged      += focused => _onFocusChanged.Emit(focused);
        _view.Closing           += ()      => _onClosing.Emit();
    }

    public nint         Handle          => _view.Handle;
    public bool         IsInitialized   => _view.IsInitialized;
    public bool         IsClosing       => _view.IsClosing;
    public double       Time            => _view.Time;
    public Vector2<int> Size            => new(_view.Size.X, _view.Size.Y);
    public Vector2<int> FramebufferSize => new(_view.FramebufferSize.X, _view.FramebufferSize.Y);

    public double FramesPerSecond  { get => _view.FramesPerSecond;  set => _view.FramesPerSecond  = value; }
    public double UpdatesPerSecond { get => _view.UpdatesPerSecond; set => _view.UpdatesPerSecond = value; }
    public bool   VSync            { get => _view.VSync;            set => _view.VSync            = value; }

    public IReadOnlySignal               OnLoad               => _onLoad.AsReadOnly();
    public IReadOnlySignal<double>       OnUpdate             => _onUpdate.AsReadOnly();
    public IReadOnlySignal<double>       OnRender             => _onRender.AsReadOnly();
    public IReadOnlySignal<Vector2<int>> OnResized            => _onResized.AsReadOnly();
    public IReadOnlySignal<Vector2<int>> OnFramebufferResize  => _onFramebufferResize.AsReadOnly();
    public IReadOnlySignal<bool>         OnFocusChanged       => _onFocusChanged.AsReadOnly();
    public IReadOnlySignal               OnClosing            => _onClosing.AsReadOnly();

    public IInputContext Input => _input ??= new SilkInputContext(_view.CreateInput());

    public void         Focus()                          => _view.Focus();
    public void         Run()                            => _view.Run(() => { });
    public void         Close()                          => _view.Close();
    public Vector2<int> PointToClient(Vector2<int> p)      => ToVec(_view.PointToClient(ToSilk(p)));
    public Vector2<int> PointToScreen(Vector2<int> p)      => ToVec(_view.PointToScreen(ToSilk(p)));
    public Vector2<int> PointToFramebuffer(Vector2<int> p) => ToVec(_view.PointToFramebuffer(ToSilk(p)));

    private static SilkVec2     ToSilk(Vector2<int> v) => new(v.X, v.Y);
    private static Vector2<int> ToVec(SilkVec2 v)      => new(v.X, v.Y);
}
