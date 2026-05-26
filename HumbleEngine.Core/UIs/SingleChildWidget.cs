namespace HumbleEngine.Core;

public abstract partial record SingleChildWidget<TChild> : Widget
    where TChild : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<TChild?> Child { get; init; }
}