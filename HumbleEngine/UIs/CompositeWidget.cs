namespace HumbleEngine;

public abstract record CompositeWidget : Widget
{
    public bool IsDirty { get; internal set; }
    public abstract Widget Build();
}