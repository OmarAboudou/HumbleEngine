namespace HumbleEngine.Core;

public class WindowNode : Node
{
    private Window _window;

    public WindowNode()
    {
        _window = Application.CreateWindowFunction!();
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _window.Dispose();
    }
}