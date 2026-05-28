namespace HumbleEngine.Core;

public abstract class UI : Node
{
    public Widget? CachedWidget { get; internal set; }

    public Widget GetWidget()
    {
        CachedWidget ??= Build();
        return CachedWidget;
    }
    
    public abstract Widget Build();
}