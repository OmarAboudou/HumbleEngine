namespace HumbleEngine.Core;

public abstract partial record Widget : HumbleRecord
{
    protected Property<T> CreateWidgetProperty<T>(
        T initialValue,
        WidgetRefreshFlag flag)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        ConnectPropertyToRefreshFlag(property, flag);
        return property;
    }

    protected void ConnectPropertyToRefreshFlag<T>(
        Property<T> property,
        WidgetRefreshFlag refreshFlag)
    {
        if (refreshFlag.HasFlag(WidgetRefreshFlag.BUILD)) 
            property.Connect(_ => MarkBuildDirty());
        
        if (refreshFlag.HasFlag(WidgetRefreshFlag.LAYOUT)) 
            property.Connect(_ => MarkLayoutDirty());

        if (refreshFlag.HasFlag(WidgetRefreshFlag.PAINT)) 
            property.Connect(_ => MarkPaintDirty());
    }

    protected ListProperty<T> CreateWidgetListProperty<T>(
        IReadOnlyList<T> initialElements,
        WidgetRefreshFlag flag)
    {
        ListProperty<T> listProperty = CreatePublicListProperty(initialElements);
        ConnectListPropertyToRefreshFlag(listProperty, flag);
        return listProperty;
    }

    protected void ConnectListPropertyToRefreshFlag<T>(
        ListProperty<T> listProperty,
        WidgetRefreshFlag flag)
    {
        if (flag.HasFlag(WidgetRefreshFlag.BUILD))
        {
            listProperty.ConnectAddedElement((_,_) => MarkBuildDirty());
            listProperty.ConnectRemovedElement((_,_) => MarkBuildDirty());
        }
        if (flag.HasFlag(WidgetRefreshFlag.LAYOUT))
        {
            listProperty.ConnectAddedElement((_,_) => MarkLayoutDirty());
            listProperty.ConnectRemovedElement((_,_) => MarkLayoutDirty());
        }
        if (flag.HasFlag(WidgetRefreshFlag.PAINT))
        {
            listProperty.ConnectAddedElement((_,_) => MarkPaintDirty());
            listProperty.ConnectRemovedElement((_,_) => MarkPaintDirty());
        }

    }

    private void MarkBuildDirty()
        => MarkDirty(WidgetRefreshFlag.BUILD);
    
    private void MarkLayoutDirty()
        => MarkDirty(WidgetRefreshFlag.LAYOUT);
    
    private void MarkPaintDirty()
        => MarkDirty(WidgetRefreshFlag.PAINT);

    private void MarkDirty(WidgetRefreshFlag flag) 
        => RefreshFlags.Value |= flag;

    public object? Key { get; init; }
    public object? GlobalKey { get; init; }

    [WidgetProperty]
    public partial Property<WidgetRefreshFlag> RefreshFlags { get; internal init; }
    
}