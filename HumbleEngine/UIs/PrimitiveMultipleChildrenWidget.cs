namespace HumbleEngine;

public abstract partial record PrimitiveMultipleChildrenWidget<TChildren> : PrimitiveWidget, IMultipleChildrenWidget<TChildren>
    where TChildren : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<TChildren> Children { get; init; }
}