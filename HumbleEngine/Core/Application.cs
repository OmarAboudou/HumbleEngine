namespace HumbleEngine;

public abstract class Application<TRoot> where TRoot : Node, IRootNode
{
    protected abstract TRoot CreateRootNode(ApplicationConfig config);

    public void Run(ApplicationConfig config)
    {
        TRoot root = CreateRootNode(config);
        root.Attach(config.Scene);
        root.EnterTree();
        root.Viewport.Run();
        root.ExitTree();
    }
}
