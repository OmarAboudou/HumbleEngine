namespace HumbleEngine.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class WidgetPropertyAttribute(WidgetRefreshFlag flag = WidgetRefreshFlag.NONE) : Attribute
{
    public WidgetRefreshFlag Flag { get; } = flag;
}