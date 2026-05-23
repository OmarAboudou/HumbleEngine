namespace HumbleEngine;

public class WindowNode : Node, IRootNode
{
    public IWindow   Window   { get; }
    public IViewport Viewport => Window;

    public WindowNode(IWindow window)
    {
        Window = window;
    }
}
