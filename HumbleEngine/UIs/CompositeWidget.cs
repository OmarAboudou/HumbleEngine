namespace HumbleEngine;

public abstract record CompositeWidget : Widget
{
    public bool IsDirty { get; internal set; }
    
    internal Widget? BuiltSubTree { get; set; }
    
    internal override IReadOnlyList<Widget> GetChildren() => BuiltSubTree is not null ? [BuiltSubTree] : [];
    
    public abstract Widget Build();
}