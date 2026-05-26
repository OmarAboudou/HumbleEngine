namespace HumbleEngine.Core;

public abstract partial record LinearLayout<TWidget> : MultipleChildrenWidget<TWidget>
    where TWidget : Widget
{
    [WidgetProperty]
    public partial Property<float> Gap { get; init; }
}