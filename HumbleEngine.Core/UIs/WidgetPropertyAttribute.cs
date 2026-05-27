namespace HumbleEngine.Core.UIs;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class WidgetPropertyAttribute(WidgetRefreshFlag flag = WidgetRefreshFlag.NONE) : Attribute
{
    public WidgetRefreshFlag Flag { get; init; } = flag;
}