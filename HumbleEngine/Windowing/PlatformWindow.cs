namespace HumbleEngine;

public abstract class PlatformWindow : HumbleObject
{
    public readonly Signal Closing;

    public readonly Signal<double> FixUpdated;

    public readonly Signal Loaded;

    public readonly Signal<double> Rendering;

    public readonly Property<string> Title;
    protected Action EmitClosing;
    protected Action<double> EmitFixUpdated;
    protected Action EmitLoaded;
    protected Action<double> EmitRendering;

    public PlatformWindow()
    {
        Title = CreatePublicProperty("DEFAULT_WINDOW_TITLE");
        Title.Connect(SetTitle);
        Loaded = CreateSignal(out EmitLoaded);
        FixUpdated = CreateSignal("delta", out EmitFixUpdated);
        Rendering = CreateSignal("delta", out EmitRendering);
        Closing = CreateSignal(out EmitClosing);
    }

    public abstract void Run();

    public abstract void Reset();

    protected abstract void SetTitle(string title);

    public override void Dispose()
    {
        base.Dispose();
        Reset();
    }
}