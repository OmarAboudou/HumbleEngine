namespace HumbleEngine;

public abstract partial record PrimitiveWidget : Widget
{
    [PrimitiveWidgetProperty]
    public partial Property<WidgetRefreshFlag> RefreshFlags { get; internal init; }

    [PrimitiveWidgetProperty]
    public partial Property<Size> Size { get; internal init; }

    [PrimitiveWidgetProperty]
    public partial Property<Position> LocalPosition { get; internal init; }

    [PrimitiveWidgetProperty]
    public partial Property<Position> ViewportPosition { get; internal init; }

    public abstract void Layout(BoxConstraints constraints);
    public virtual void Paint(PaintCommandBuffer buffer, Position offset) { }

    protected Property<T> CreateWidgetProperty<T>(T initialValue, WidgetRefreshFlag flag)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        ConnectWidgetProperty(property, flag);
        return property;
    }

    protected void ConnectWidgetProperty<T>(Property<T> property, WidgetRefreshFlag flag)
    {
        if (flag.HasFlag(WidgetRefreshFlag.LAYOUT))
            property.Connect(_ => MarkDirty(WidgetRefreshFlag.LAYOUT));

        if (flag.HasFlag(WidgetRefreshFlag.PAINT))
            property.Connect(_ => MarkDirty(WidgetRefreshFlag.PAINT));
    }

    protected ListProperty<T> CreateWidgetListProperty<T>(IReadOnlyList<T>? initialElements, WidgetRefreshFlag flag)
    {
        ListProperty<T> listProperty = CreatePublicListProperty(initialElements);
        ConnectWidgetListProperty(listProperty, flag);
        return listProperty;
    }

    protected void ConnectWidgetListProperty<T>(ListProperty<T> listProperty, WidgetRefreshFlag flag)
    {
        if (flag.HasFlag(WidgetRefreshFlag.LAYOUT))
        {
            listProperty.ConnectAddedElement((_, _) => MarkDirty(WidgetRefreshFlag.LAYOUT));
            listProperty.ConnectRemovedElement((_, _) => MarkDirty(WidgetRefreshFlag.LAYOUT));
        }

        if (flag.HasFlag(WidgetRefreshFlag.PAINT))
        {
            listProperty.ConnectAddedElement((_, _) => MarkDirty(WidgetRefreshFlag.PAINT));
            listProperty.ConnectRemovedElement((_, _) => MarkDirty(WidgetRefreshFlag.PAINT));
        }
    }

    private void MarkDirty(WidgetRefreshFlag flag) => RefreshFlags.Value |= flag;
}