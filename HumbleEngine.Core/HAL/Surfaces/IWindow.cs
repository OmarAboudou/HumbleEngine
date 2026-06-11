namespace HumbleEngine;

public interface IWindow : IGraphicsSurface
{
    void Show();
    void Hide();
    void SetTitle(string title);
    void Resize(int width, int height);
    event Action<int, int>? OnResize;
    IWindow CreateChildWindow(WindowDescription description);
}
