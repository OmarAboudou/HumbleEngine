namespace HumbleEngine.Core;

public abstract partial record Widget : HumbleRecord
{
    protected Property<T> CreatePublicProperty<T>(T initialValue, DirtyFlag flag = DirtyFlag.NONE)
    {
        Property<T> property = base.CreatePublicProperty(initialValue);
        return ConnectFlag(flag, property);
    }

    protected Property<T> ConnectFlag<T>(DirtyFlag flag, Property<T> property)
    {
        if (flag.HasFlag(DirtyFlag.PAINT))
        {
            property.Connect( _ => Flag.Value &= DirtyFlag.PAINT );
        }
        if (flag.HasFlag(DirtyFlag.LAYOUT))
        {
            property.Connect( _ => Flag.Value &= DirtyFlag.LAYOUT );
        }
        return property;
    }
    
    protected ListProperty<T> CreatePublicListProperty<T>(IReadOnlyList<T>? initialElements = null, DirtyFlag flag = DirtyFlag.NONE)
    {
        ListProperty<T> property = base.CreatePublicListProperty(initialElements);
        return ConnectFlag(flag, property);
    }

    protected ListProperty<T> ConnectFlag<T>(DirtyFlag flag, ListProperty<T> property)
    {
        if (flag.HasFlag(DirtyFlag.PAINT))
        {
            property.ConnectAddedElement( (_,_) => Flag.Value &= DirtyFlag.PAINT );
            property.ConnectRemovedElement( (_,_) => Flag.Value &= DirtyFlag.PAINT );
        }
        if (flag.HasFlag(DirtyFlag.LAYOUT))
        {
            property.ConnectAddedElement( (_,_) => Flag.Value &= DirtyFlag.LAYOUT );
            property.ConnectRemovedElement( (_,_) => Flag.Value &= DirtyFlag.LAYOUT );
        }
        return property;
    }
    
 
    public object? Key { get ; init; }
    
    [WidgetProperty]
    internal partial Property<DirtyFlag> Flag { get; init; }
    
    [WidgetProperty(DirtyFlag.LAYOUT)]
    public partial Property<Length> Width { get; init; }
    
    [WidgetProperty(DirtyFlag.LAYOUT)]
    public partial Property<Length> Height { get; init; }

    public void Layout(BoxConstraints boxConstraints)
    {
        
    }

    public abstract void PerformLayout();
}

public readonly record struct BoxConstraints(
    float MinWidth,
    float? MaxWidth,
    float MinHeight,
    float? MaxHeight);

[Flags]
public enum DirtyFlag
{
    NONE = 0,
    LAYOUT = 1 << 0,
    PAINT = 1 << 1,
}