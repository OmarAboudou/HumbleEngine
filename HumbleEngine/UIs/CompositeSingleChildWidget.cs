namespace HumbleEngine;

public abstract partial record CompositeSingleChildWidget<TChild> : CompositeWidget, ISingleChildWidget<TChild>
    where TChild : Widget
{
    [CompositeWidgetProperty]
    public partial Property<TChild?> Child { get; init; }
}