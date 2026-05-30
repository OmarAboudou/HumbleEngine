namespace HumbleEngine;

public abstract partial record CompositeMultiChildWidget<TChildren> : CompositeWidget, IMultiChildWidget<TChildren>
    where TChildren : Widget
{
    [CompositeWidgetProperty]
    public partial ListProperty<TChildren> Children { get; init; }
}