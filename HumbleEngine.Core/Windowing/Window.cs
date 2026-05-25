namespace HumbleEngine.Core;

public abstract class Window : IDisposable
{
    public Window()
    {
        Title.ValueChanged.Connect(SetTitle);
        Loaded = new Signal(out EmitLoaded);
    }

    public abstract void Initialize();
    
    
    public readonly Property<string> Title = new("Humble Engine Window");

    
    public readonly Signal Loaded;
    
    private Action EmitLoaded;
    
    public void OnLoaded() => EmitLoaded();


    public abstract void Run();

    public abstract void Reset();
    
    protected abstract void SetTitle(string title);

    public void Dispose() 
        => Reset();
}