namespace HumbleEngine.Core;

public class Window : Node
{
    public Window() : this(null) { }
    public Window(PlatformWindow platformWindow)
    {
        Title = CreatePublicProperty("Humble Platform Window");
        _platformWindow = platformWindow;
        Title.Bind2WayTo(_platformWindow.Title);
    }

    private readonly PlatformWindow _platformWindow;

    public readonly Property<string> Title;
    
    public override void Dispose()
    {
        base.Dispose();
        _platformWindow.Dispose();
    }
}