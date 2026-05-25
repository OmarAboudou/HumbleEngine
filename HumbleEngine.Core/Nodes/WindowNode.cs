namespace HumbleEngine.Core;

public class WindowNode : Node
{
    internal Window _window;
    
    public Property<string> Title { get; }

    public WindowNode()
    {
        _window = Application.CreateWindowFunction!();
        Title = _window.Title;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _window.Dispose();
    }
}