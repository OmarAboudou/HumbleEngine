namespace HumbleEngine.Core;

public abstract class Application
{

    protected abstract Window CreateWindow();
    
    public void Run(ApplicationConfig config)
    {
        using Window window = CreateWindow();
        WindowNode windowNode = new(window)
        {
            config.scene
        };
        window.Loaded.Connect( () => Console.WriteLine("LOADED !") );
        window.FixUpdated.Connect( (delta) => Console.WriteLine($"Fix Update {1/delta}/s") );
        window.Rendering.Connect((delta) => windowNode.Title.Value = $"{1.0 / delta}fps");
        window.Closing.Connect( () => Console.WriteLine("CLOSING !") );
        window.Run();
        
    }
}