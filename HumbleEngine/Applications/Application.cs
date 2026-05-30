namespace HumbleEngine;

public abstract class Application
{
    protected abstract PlatformWindow CreatePlatformWindow(ApplicationConfig config);
    protected virtual IRenderer? CreateRenderer() => null;
    protected virtual Widget?    BuildRootWidget() => null;

    // TODO: source generator — injecter les passes custom [UpdatePass]/[FixedUpdatePass] ici
    protected virtual IEnumerable<IUpdatePass>      GetCustomUpdatePasses()      => [];
    protected virtual IEnumerable<IFixedUpdatePass> GetCustomFixedUpdatePasses() => [];

    internal static Func<PlatformWindow> PlatformWindowFactory = null!;

    public void Run(ApplicationConfig config)
    {
        PlatformWindowFactory = () => CreatePlatformWindow(config);

        using Window window = new();
        if (config.Scene is not null)
            window.Add(config.Scene);

        PlatformWindow     platformWindow = window.PlatformWindow;
        IRenderer?         renderer       = CreateRenderer();
        Widget?            rootWidget     = BuildRootWidget();
        PaintCommandBuffer paintBuffer    = new();
        Size               windowSize     = new(config.Width, config.Height);

        List<IUpdatePass>      updatePasses      = [new UpdatePass(), ..GetCustomUpdatePasses()];
        List<IFixedUpdatePass> fixedUpdatePasses = [new FixedUpdatePass(), ..GetCustomFixedUpdatePasses()];

        platformWindow.Loaded.Connect(() =>
        {
            if (renderer is not null)
            {
                if (!renderer.Supports(config.PreferredBackend))
                    throw new InvalidOperationException($"Renderer does not support {config.PreferredBackend}.");
                renderer.Initialize(config.PreferredBackend);
                renderer.Resize(windowSize);
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

                if (rootWidget is not null)
                {
                    paintBuffer.Clear();
                    MountPass.Mount(rootWidget);
                    LayoutPass.Layout(rootWidget, BoxConstraints.Loose(windowSize));
                    PaintPass.Paint(rootWidget, paintBuffer);
                    renderer?.Render(paintBuffer);
                }
            });
        });

        platformWindow.Resized.Connect(size =>
        {
            windowSize = size;
            renderer?.Resize(size);
        });

        platformWindow.Closing.Connect(() =>
        {
            renderer?.Dispose();
            Console.WriteLine("CLOSING !");
        });

        platformWindow.Run();
    }
}
