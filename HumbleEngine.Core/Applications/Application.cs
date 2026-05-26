namespace HumbleEngine.Core;

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
        platformWindow.Loaded.Connect(() =>
        {
            platformWindow.FixUpdated.Connect(delta =>
            {
                foreach (IFixedUpdatePass fixedUpdatePass in config.FixedUpdatePasses)
                    fixedUpdatePass.Execute(window, delta);
            });
            platformWindow.Rendering.Connect(delta =>
            {
                foreach (IUpdatePass updatePass in config.UnderPasses) updatePass.Execute(window, delta);
            });
        });
        platformWindow.Closing.Connect(() => Console.WriteLine("CLOSING !"));
        platformWindow.Run();
    }
}