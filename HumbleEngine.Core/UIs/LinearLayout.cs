namespace HumbleEngine.Core;

public abstract partial record LinearLayout : MultipleChildrenWidget<Widget>
{
    [WidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<MainAxisAlignment> MainAxisAlignment { get; init; }
    
    [WidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<CrossAxisAlignment> CrossAxisAlignment { get; init; }
    
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<MainAxisSize> MainAxisSize { get; init; }

}