namespace HumbleEngine.Core;

public class Window : Node
{
    public Window() : this(null) { }
    public Window(PlatformWindow platformWindow)
    {
        Title = CreateProperty("Humble Platform Window");
        PlatformWindow = platformWindow;
        Title.Bind2WayTo(PlatformWindow.Title);
    }
    
    internal PlatformWindow PlatformWindow
    {
        get;
        private init;
    }

    public EditableProperty<string> Title { get; }
    
    public override void Dispose()
    {
        base.Dispose();
        PlatformWindow.Dispose();
    }
}