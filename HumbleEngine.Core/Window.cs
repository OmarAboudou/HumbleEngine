namespace HumbleEngine.Core;

public abstract class Window : IDisposable
{
    public Property<string> Title { get; init; } = new("Humble Engine Window");
    public abstract void Initialize();

    public abstract void Run();

    public abstract void Reset();


    public void Dispose()
    {
        Reset();
    }
}