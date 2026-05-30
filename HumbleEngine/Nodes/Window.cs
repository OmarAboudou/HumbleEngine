namespace HumbleEngine;

public class Window : Node
{
    internal PlatformWindow PlatformWindow;

    public readonly Property<string> Title;

    public Window()
    {
        Title = CreatePublicProperty("Humble Platform Window");
        PlatformWindow = Application.PlatformWindowFactory();
        Title.Bind2WayTo(PlatformWindow.Title);
    }

    public override void Dispose()
    {
        base.Dispose();
        PlatformWindow.Dispose();
    }
}