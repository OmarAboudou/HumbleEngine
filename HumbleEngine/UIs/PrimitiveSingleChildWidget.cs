namespace HumbleEngine;

public abstract partial record PrimitiveSingleChildWidget<TChild> : PrimitiveWidget, ISingleChildWidget<TChild>
    where TChild : Widget
{
    internal override IReadOnlyList<Widget> GetChildren() => Child.Value is not null ? [Child.Value] : [];

    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<TChild?> Child { get; init; }
}