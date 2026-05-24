namespace HumbleEngine;

public abstract class Application<TRoot> where TRoot : Node, IRootNode
{
    protected TRoot?              Root   { get; private set; }
    protected ApplicationConfig?  Config { get; private set; }

    protected abstract TRoot CreateRootNode(ApplicationConfig config);

    public void Run(ApplicationConfig config)
    {
        Config     = config;
        Root       = CreateRootNode(config);
        Root.Attach(config.Scene);
        Root.EnterTree();

        Root.Viewport.OnUpdate.Connect(delta =>
        {
            foreach (var pass in config.Passes)
                if (pass.ShouldExecute())
                    pass.Execute(Root, delta);
        });

        Root.Viewport.Run();
        Root.ExitTree();
    }

    protected void ConnectRenderPasses(IRenderer renderer)
    {
        if (Config is null || Root is null) return;

        renderer.OnBeginFrame.Connect(canvas =>
        {
            var ctx = new RenderContext(renderer, canvas);
            foreach (var pass in Config.RenderPasses)
                if (pass.ShouldExecute())
                    pass.Execute(Root, ctx);
        });
    }
}
