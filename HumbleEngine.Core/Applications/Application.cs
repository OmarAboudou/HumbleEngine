namespace HumbleEngine.Core;

public abstract class Application
{
    protected abstract PlatformWindow CreatePlatformWindow();

    public void Run(ApplicationConfig config)
    {
        using PlatformWindow platformWindow = CreatePlatformWindow();
        using Window window = new(platformWindow);
        window.Add(config.Scene);
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