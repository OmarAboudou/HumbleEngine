namespace HumbleEngine.Core;

public abstract class PlatformWindow : HumbleObject
{
    protected PlatformWindow()
    {
        Title = CreateProperty("DEFAULT_WINDOW_TITLE");
        Title.Connect(SetTitle);
        Loaded = CreateSignal(out EmitLoaded);
        FixUpdated = CreateSignal("delta", out EmitFixUpdated);
        Rendering = CreateSignal("delta", out EmitRendering);
        Closing = CreateSignal(out EmitClosing);
    }

    public readonly EditableProperty<string> Title;
    
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

    public override void Dispose()
    {
        base.Dispose();
        Reset();
    }
}