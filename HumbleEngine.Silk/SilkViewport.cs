using HumbleEngine;
using SilkView = Silk.NET.Windowing.IView;

namespace HumbleEngine.Silk;

public class SilkViewport : IViewport
{
    protected readonly SilkView _view;

    private readonly Signal<double>       _onUpdate  = new();
    private readonly Signal<double>       _onRender  = new();
    private readonly Signal<Vector2<int>> _onResized = new();
    private readonly Signal               _onClosing = new();

    public SilkViewport(SilkView view)
    {
        _view = view;
        _view.Update  += delta => _onUpdate.Emit(delta);
        _view.Render  += delta => _onRender.Emit(delta);
        _view.Resize  += size  => _onResized.Emit(new Vector2<int>(size.X, size.Y));
        _view.Closing += ()    => _onClosing.Emit();
    }

    public Vector2<int> Size => new(_view.Size.X, _view.Size.Y);
    public double  FramesPerSecond  { get => _view.FramesPerSecond;  set => _view.FramesPerSecond  = value; }
    public double  UpdatesPerSecond { get => _view.UpdatesPerSecond; set => _view.UpdatesPerSecond = value; }
    public bool    VSync            { get => _view.VSync;            set => _view.VSync            = value; }

    public IReadOnlySignal<double>       OnUpdate  => _onUpdate.AsReadOnly();
    public IReadOnlySignal<double>       OnRender  => _onRender.AsReadOnly();
    public IReadOnlySignal<Vector2<int>> OnResized => _onResized.AsReadOnly();
    public IReadOnlySignal               OnClosing => _onClosing.AsReadOnly();

    public void Run()   => _view.Run(() => { });
    public void Close() => _view.Close();
}
