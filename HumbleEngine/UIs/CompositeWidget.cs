namespace HumbleEngine;

public abstract record CompositeWidget : Widget
{
    public bool IsDirty { get; internal set; }
    
    internal Widget? BuiltSubTree { get; set; }
    
    public abstract Widget Build();
}