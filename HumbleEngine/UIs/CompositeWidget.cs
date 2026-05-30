namespace HumbleEngine;

public abstract record CompositeWidget : Widget
{
    public abstract Widget Build();
}