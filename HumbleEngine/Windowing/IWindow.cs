namespace HumbleEngine;

public interface IWindow : IViewport
{
    string       Title        { get; set; }
    Vector2<int> Position     { get; set; }
    WindowState  WindowState  { get; set; }
    WindowBorder WindowBorder { get; set; }
    bool         IsVisible    { get; set; }
    bool         TopMost      { get; set; }
    IWindow?  Parent     { get; }
    IMonitor? Monitor    { get; }
    Insets BorderSize { get; }

    IReadOnlySignal<Vector2<int>> OnMove         { get; }
    IReadOnlySignal<WindowState>  OnStateChanged { get; }
    IReadOnlySignal<string[]>     OnFileDrop     { get; }

    IWindow CreateChildWindow(WindowOptions options);
    void    SetWindowIcon(ReadOnlySpan<RawImage> icons);
}
