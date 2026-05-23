namespace HumbleEngine;

public interface IViewport
{
    Vector2<int> Size { get; }

    double FramesPerSecond  { get; set; }
    double UpdatesPerSecond { get; set; }
    bool   VSync            { get; set; }

    IReadOnlySignal<double>  OnUpdate  { get; }
    IReadOnlySignal<double>  OnRender  { get; }
    IReadOnlySignal<Vector2<int>> OnResized { get; }
    IReadOnlySignal          OnClosing { get; }

    void Run();
    void Close();
}
