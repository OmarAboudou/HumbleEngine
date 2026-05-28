namespace HumbleEngine.Core;

public abstract partial record Widget : HumbleRecord
{
    protected Property<T> CreateWidgetProperty<T>(
        T initialValue = default,
        WidgetRefreshFlag flags = WidgetRefreshFlag.NONE)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        ConnectWidgetProperty(property, flags);
        return property;
    }

    protected void ConnectWidgetProperty<T>(Property<T> property,
        WidgetRefreshFlag flags = WidgetRefreshFlag.NONE)
    {
        if (flags.HasFlag(WidgetRefreshFlag.LAYOUT)) 
            property.Connect(_ => RefreshFlag.Value |= WidgetRefreshFlag.LAYOUT);
        
        if (flags.HasFlag(WidgetRefreshFlag.PAINT)) 
            property.Connect(_ => RefreshFlag.Value |= WidgetRefreshFlag.PAINT);
    }

    protected  ListProperty<T> CreateWidgetListProperty<T>(
        IReadOnlyList<T> initialElements = null,
        WidgetRefreshFlag flags = WidgetRefreshFlag.NONE)
    {
        ListProperty<T> listProperty = CreatePublicListProperty(initialElements);
        ConnectWidgetListProperty(listProperty, flags);
        return listProperty;
    }

    protected void ConnectWidgetListProperty<T>(ListProperty<T> listProperty,
        WidgetRefreshFlag flags = WidgetRefreshFlag.NONE)
    {
        if (flags.HasFlag(WidgetRefreshFlag.LAYOUT))
        {
            listProperty.ConnectAddedElement( (_, _) => RefreshFlag.Value |= WidgetRefreshFlag.LAYOUT);
            listProperty.ConnectRemovedElement( (_, _) => RefreshFlag.Value |= WidgetRefreshFlag.LAYOUT);
        }
        if (flags.HasFlag(WidgetRefreshFlag.PAINT))
        {
            listProperty.ConnectAddedElement( (_, _) => RefreshFlag.Value |= WidgetRefreshFlag.PAINT);
            listProperty.ConnectRemovedElement( (_, _) => RefreshFlag.Value |= WidgetRefreshFlag.PAINT);
        }
    }
    
    public object? Key { get; init; }

    [WidgetProperty] 
    public partial Property<WidgetRefreshFlag> RefreshFlag { get; internal init; }
    
    [WidgetProperty]
    public partial Property<Size> Size { get; internal init; }
    
    [WidgetProperty]
    public partial Property<Offset> Offset { get; internal init; }
}