namespace HumbleEngine;

public abstract partial record CompositeSingleChildWidget<TChild> : CompositeWidget, ISingleChildWidget<TChild>
    where TChild : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<TChild?> Child { get; init; }
}