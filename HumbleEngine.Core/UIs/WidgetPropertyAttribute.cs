namespace HumbleEngine.Core;

[AttributeUsage(AttributeTargets.Property)]
public class WidgetPropertyAttribute(WidgetRefreshFlag flag = WidgetRefreshFlag.NONE) : Attribute
{
    public WidgetRefreshFlag Flag { get; init; } = flag;
}