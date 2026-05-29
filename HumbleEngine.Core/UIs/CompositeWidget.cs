namespace HumbleEngine.Core;

public abstract record CompositeWidget : Widget
{
    public abstract Widget Build();
}