namespace HumbleEngine;

public abstract class Application<TRoot> where TRoot : Node, IRootNode
{
    protected TRoot?             Root     { get; private set; }
    protected ApplicationConfig? Config   { get; private set; }
    protected IRenderer?         Renderer { get; private set; }

    private readonly BlackBoard  _board = new();
    private IFixedUpdatePass[]? _fixedUpdatePasses;
    private IUpdatePass[]?      _updatePasses;
    private IRenderPass[]?      _renderPasses;

    protected abstract TRoot      CreateRootNode(ApplicationConfig config);
    protected abstract IRenderer? CreateRenderer(GraphicsAPI api);

    public void Run(ApplicationConfig config)
    {
        Config             = config;
        _fixedUpdatePasses = config.Passes.OfType<IFixedUpdatePass>().ToArray();
        _updatePasses      = config.Passes.OfType<IUpdatePass>().ToArray();
        _renderPasses      = config.Passes.OfType<IRenderPass>().ToArray();

        Root = CreateRootNode(config);
        Root.Attach(config.Scene);
        Root.EnterTree();

        Root.Viewport.OnLoad.Connect(() =>
        {
            var renderer = CreateRenderer(config.Api);
            if (renderer is null) return;
            Renderer = renderer;
            renderer.Attach(Root.Viewport);
            renderer.OnBeginFrame.Connect(canvas =>
            {
                var ctx = new RenderContext(renderer, canvas);
                foreach (var pass in _renderPasses!)
                    if (pass.ShouldExecute())
                        pass.Execute(Root, ctx, _board);
            });
        });

        Root.Viewport.OnUpdate.Connect(delta =>
        {
            foreach (var pass in _fixedUpdatePasses!)
                if (pass.ShouldExecute())
                    pass.Execute(Root, delta, _board);
        });

        Root.Viewport.OnRender.Connect(delta =>
        {
            foreach (var pass in _updatePasses!)
                if (pass.ShouldExecute())
                    pass.Execute(Root, delta, _board);
        });

        Root.Viewport.OnClosing.Connect(() =>
        {
            Renderer?.Detach();
            Renderer = null;
        });

        Root.Viewport.Run();
        Root.ExitTree();
    }
}
