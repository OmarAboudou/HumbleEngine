namespace HumbleEngine.Core;

public class WindowNode : Node
{
    public WindowNode(){}
    public WindowNode(Window window)
    {
        Window = window;
    }
    
    internal Window Window
    {
        get;
        init
        {
            field = value;
            field.Title.Bind2Way(Title);
        }
    }

    public EditableProperty<string> Title { get; } = new("DEFAULT_WINDOW_TITLE");
    
    public override void Dispose()
    {
        base.Dispose();
        Window.Dispose();
    }
}