namespace HumbleEngine;

public abstract partial record PrimitiveSingleChildWidget<TChild> : PrimitiveWidget, ISingleChildWidget<TChild>
    where TChild : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<TChild?> Child { get; init; }
}