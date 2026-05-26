namespace HumbleEngine.Core;

public abstract record Widget : HumbleRecord
{
    public DirtyFlag Flag { get; internal set; } = DirtyFlag.NONE;
    
    protected Property<T> CreatePublicProperty<T>(T initialValue, DirtyFlag flag = DirtyFlag.NONE)
    {
        Property<T> property = base.CreatePublicProperty(initialValue);
        return ConnectFlag(flag, property);
    }

    protected Property<T> ConnectFlag<T>(DirtyFlag flag, Property<T> property)
    {
        if (flag.HasFlag(DirtyFlag.PAINT))
        {
            property.Connect( (_ => Flag &= DirtyFlag.PAINT ) );
        }
        if (flag.HasFlag(DirtyFlag.LAYOUT))
        {
            property.Connect( (_ => Flag &= DirtyFlag.LAYOUT ) );
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
            property.ConnectAddedElement( ( (_,_) => Flag &= DirtyFlag.PAINT ) );
            property.ConnectRemovedElement( ( (_,_) => Flag &= DirtyFlag.PAINT ) );
        }
        if (flag.HasFlag(DirtyFlag.LAYOUT))
        {
            property.ConnectAddedElement( ( (_,_) => Flag &= DirtyFlag.LAYOUT ) );
            property.ConnectRemovedElement( ( (_,_) => Flag &= DirtyFlag.LAYOUT ) );
        }
        return property;
    }
    
    public Property<object?> KeyProperty { get => field ??= CreatePublicProperty<object?>(null); init; }
    
    public Property<Length> WidthProperty { get => field ??= CreatePublicProperty<Length>(100.Percent(), DirtyFlag.LAYOUT); init; }
    public Property<Length> HeightProperty { get => field ??= CreatePublicProperty<Length>(100.Percent(), DirtyFlag.LAYOUT); init; }
}

public readonly record struct LayoutResult(
    float Width,
    float Height);

public readonly record struct LayoutConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints);

[Flags]
public enum DirtyFlag
{
    NONE = 0,
    LAYOUT = 1 << 0,
    PAINT = 1 << 1,
}