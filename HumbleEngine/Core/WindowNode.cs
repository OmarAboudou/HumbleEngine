namespace HumbleEngine;

public class WindowNode : Node
{
    public IWindow Window { get; }

    public WindowNode(IWindow window)
    {
        Window = window;
    }
}
