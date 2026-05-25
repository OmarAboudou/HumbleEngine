namespace HumbleEngine.Core;

public abstract class Application
{
    public Application()
    {
        CreateWindowFunction = CreateWindow;
    }

    protected abstract Window CreateWindow();
    
    protected internal static Func<Window>? CreateWindowFunction { get; internal set; }

    public void Run(Node root)
    {
        WindowNode windowNode = new()
        {
            root
        };
        windowNode.Window.Run();
    }
}