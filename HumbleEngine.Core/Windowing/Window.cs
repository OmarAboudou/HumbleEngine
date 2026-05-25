namespace HumbleEngine.Core;

public abstract class Window : IDisposable
{
    public Window()
    {
        Title.Connect(SetTitle);
        Loaded = new Signal(out EmitLoaded);
        FixUpdated = new Signal<double>("delta", out EmitFixUpdated);
        Rendering = new Signal<double>("delta", out EmitRendering);
        Closing = new Signal(out EmitClosing);
    }

    // public abstract void Initialize();

    public readonly EditableProperty<string> Title = new("Humble Engine Window");
    
    public readonly Signal Loaded;
    protected Action EmitLoaded;
    
    public readonly Signal<double> FixUpdated;
    protected Action<double> EmitFixUpdated;
    
    public readonly Signal<double> Rendering;
    protected Action<double> EmitRendering;
    
    public readonly Signal Closing;
    protected Action EmitClosing;

    public abstract void Run();

    public abstract void Reset();

    protected abstract void SetTitle(string title);

    public void Dispose() => Reset();
}