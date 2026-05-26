namespace HumbleEngine.Core;

public class Window : Node
{
    private readonly PlatformWindow _platformWindow;

    public readonly Property<string> Title;

    public Window() : this(null)
    {
    }

    public Window(PlatformWindow platformWindow)
    {
        Title = CreatePublicProperty("Humble Platform Window");
        _platformWindow = platformWindow;
        Title.Bind2WayTo(_platformWindow.Title);
    }

    public override void Dispose()
    {
        base.Dispose();
        _platformWindow.Dispose();
    }
}