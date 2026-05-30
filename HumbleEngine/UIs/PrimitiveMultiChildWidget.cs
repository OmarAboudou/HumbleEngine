namespace HumbleEngine;

public abstract partial record PrimitiveMultiChildWidget<TChildren> : PrimitiveWidget, IMultiChildWidget<TChildren>
    where TChildren : Widget
{
    internal override IReadOnlyList<Widget> GetChildren() => Children.Listener;
    
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<TChildren> Children { get; init; }
}