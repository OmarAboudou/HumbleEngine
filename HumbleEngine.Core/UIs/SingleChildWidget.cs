namespace HumbleEngine.Core;

public abstract partial record SingleChildWidget<TChild> : Widget
    where TChild : Widget
{
    [WidgetProperty(DirtyFlag.LAYOUT | DirtyFlag.PAINT)]
    public partial Property<TChild?> Child { get; init; }
}