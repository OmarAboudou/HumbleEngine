namespace HumbleEngine;

public abstract class Application
{
    public Application()
    {
        PlatformWindowFactory = CreatePlatformWindow;
    }
    
    protected abstract PlatformWindow CreatePlatformWindow();
    
    internal static Func<PlatformWindow> PlatformWindowFactory;

    public void Run(ApplicationConfig config)
    {
        using Window window = new();
        window.Add(config.Scene);
        PlatformWindow platformWindow = window.PlatformWindow;

        IRenderer? renderer = config.RendererFactory?.Invoke();

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
                foreach (IFixedUpdatePass fixedUpdatePass in config.FixedUpdatePasses)
                    fixedUpdatePass.Execute(window, delta);
            });
            platformWindow.Rendering.Connect(delta =>
            {
                foreach (IUpdatePass updatePass in config.UnderPasses)
                    updatePass.Execute(window, delta);
                // TODO: déclencher le paint pass ici, puis renderer.Render(buffer)
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