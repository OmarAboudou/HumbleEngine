namespace HumbleEngine;

public abstract class Application
{
    public Application()
    {
        PlatformWindowFactory = CreatePlatformWindow;
    }

    protected abstract PlatformWindow CreatePlatformWindow();
    protected virtual IRenderer? CreateRenderer() => null;

    // TODO: source generator — injecter les passes custom [UpdatePass]/[FixedUpdatePass] ici
    protected virtual IEnumerable<IUpdatePass>      GetCustomUpdatePasses()      => [];
    protected virtual IEnumerable<IFixedUpdatePass> GetCustomFixedUpdatePasses() => [];

    internal static Func<PlatformWindow> PlatformWindowFactory;

    public void Run(ApplicationConfig config)
    {
        using Window window = new();
        if (config.Scene is not null)
            window.Add(config.Scene);

        PlatformWindow platformWindow = window.PlatformWindow;
        IRenderer?     renderer       = CreateRenderer();

        List<IUpdatePass>      updatePasses      = [new UpdatePass(), ..GetCustomUpdatePasses()];
        List<IFixedUpdatePass> fixedUpdatePasses = [new FixedUpdatePass(), ..GetCustomFixedUpdatePasses()];

        platformWindow.Loaded.Connect(() =>
        {
            if (renderer is not null)
            {
                if (!renderer.Supports(config.PreferredBackend))
                    throw new InvalidOperationException($"Renderer does not support {config.PreferredBackend}.");
                renderer.Initialize(config.PreferredBackend);
            }

            platformWindow.FixUpdated.Connect(delta =>
            {
                foreach (IFixedUpdatePass pass in fixedUpdatePasses)
                    pass.Execute(window, delta);
            });

            platformWindow.Rendering.Connect(delta =>
            {
                foreach (IUpdatePass pass in updatePasses)
                    pass.Execute(window, delta);
                // TODO: paint pass → renderer.Render(buffer)
            });
        });

        if (renderer is not null)
            platformWindow.Resized.Connect(renderer.Resize);

        platformWindow.Closing.Connect(() =>
        {
            renderer?.Dispose();
            Console.WriteLine("CLOSING !");
        });

        platformWindow.Run();
    }
}
