namespace HumbleEngine;

public abstract class Application
{
    protected abstract Node CreateRootNode(ApplicationConfig config);
    protected abstract void RunLoop();

    public void Run(ApplicationConfig config)
    {
        var root = CreateRootNode(config);
        root.Attach(config.Scene);
        root.EnterTree();
        RunLoop();
        root.ExitTree();
    }
}
