namespace HumbleEngine.Core;

public class WindowNode : Node
{
    internal readonly Window Window;
    
    public Property<string> Title { get; }

    public WindowNode()
    {
        Window = Application.CreateWindowFunction!();
        Title = Window.Title;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        Window.Dispose();
    }
}