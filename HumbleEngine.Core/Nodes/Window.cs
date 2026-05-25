namespace HumbleEngine.Core;

public class Window : Node
{
    public Window() : this(null) { }
    public Window(PlatformWindow platformWindow)
    {
        Title = CreateProperty("Humble PlatformWindow");
        PlatformWindow = platformWindow;
    }
    
    internal PlatformWindow PlatformWindow
    {
        get;
        private init
        {
            field = value;
            field.Title.Bind2Way(Title);
        }
    }

    public EditableProperty<string> Title { get; }
    
    public override void Dispose()
    {
        base.Dispose();
        PlatformWindow.Dispose();
    }
}