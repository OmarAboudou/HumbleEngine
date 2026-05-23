namespace HumbleEngine;

public abstract class Application<TRoot> where TRoot : Node, IRootNode
{
    protected abstract TRoot CreateRootNode(ApplicationConfig config);

    public void Run(ApplicationConfig config)
    {
        TRoot root = CreateRootNode(config);
        root.Attach(config.Scene);
        root.EnterTree();

        root.Viewport.OnUpdate.Connect(delta =>
        {
            var ctx = new PassContext(root, delta);
            foreach (var pass in config.Passes)
                if (pass.ShouldExecute())
                    pass.Execute(ctx);
        });

        root.Viewport.Run();
        root.ExitTree();
    }
}
