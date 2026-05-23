namespace HumbleEngine;

public interface IViewport
{
    nint         Handle          { get; }
    bool         IsInitialized   { get; }
    bool         IsClosing       { get; }
    double       Time            { get; }
    Vector2<int> Size            { get; }
    Vector2<int> FramebufferSize { get; }

    double FramesPerSecond  { get; set; }
    double UpdatesPerSecond { get; set; }
    bool   VSync            { get; set; }

    IReadOnlySignal              OnLoad              { get; }
    IReadOnlySignal<double>      OnUpdate            { get; }
    IReadOnlySignal<double>      OnRender            { get; }
    IReadOnlySignal<Vector2<int>> OnResized          { get; }
    IReadOnlySignal<Vector2<int>> OnFramebufferResize { get; }
    IReadOnlySignal<bool>        OnFocusChanged      { get; }
    IReadOnlySignal              OnClosing           { get; }

    IGraphicsContext? GraphicsContext { get; }
    IInputContext     Input           { get; }

    void         Focus();
    void         Run();
    void         Close();
    Vector2<int> PointToClient(Vector2<int> point);
    Vector2<int> PointToScreen(Vector2<int> point);
    Vector2<int> PointToFramebuffer(Vector2<int> point);
}
