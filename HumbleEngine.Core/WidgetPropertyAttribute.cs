namespace HumbleEngine.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class WidgetPropertyAttribute(DirtyFlag flag = DirtyFlag.NONE) : Attribute
{
    public DirtyFlag Flag { get; } = flag;
}