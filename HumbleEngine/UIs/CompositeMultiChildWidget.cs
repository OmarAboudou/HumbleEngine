namespace HumbleEngine;

public abstract partial record CompositeMultiChildWidget<TChildren> : CompositeWidget, IMultiChildWidget<TChildren>
    where TChildren : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<TChildren> Children { get; init; }
}