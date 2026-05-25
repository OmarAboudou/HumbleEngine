namespace HumbleEngine.Core;

public abstract class Application
{
    
    protected abstract PlatformWindow CreatePlatformWindow();
    
    public void Run(ApplicationConfig config)
    { ;
        PlatformWindow platformWindow = CreatePlatformWindow();
        Window window = new(platformWindow)
        {
            config.scene
        };
        platformWindow.Loaded.Connect( () => Console.WriteLine("LOADED !") );
        platformWindow.FixUpdated.Connect( (delta) => Console.WriteLine($"Fix Update {1/delta}/s") );
        platformWindow.Rendering.Connect((delta) => window.Title.Value = $"{1.0 / delta}fps");
        platformWindow.Closing.Connect( () => Console.WriteLine("CLOSING !") );
        platformWindow.Run();
        
    }
}