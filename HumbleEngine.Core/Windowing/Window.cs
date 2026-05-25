namespace HumbleEngine.Core;

public abstract class Window : IDisposable
{
    public Window()
    {
        Title.Connect(SetTitle);
    }
    
    public Property<string> Title { get; init; } = new("Humble Engine Window");
    public abstract void Initialize();

    public abstract void Run();

    public abstract void Reset();
    
    protected abstract void SetTitle(string title);

    public void Dispose()
    {
        Reset();
    }
}