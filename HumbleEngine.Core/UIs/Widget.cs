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
    public partial Property<Size> DesiredSize { get; internal init; }
    
    [WidgetProperty]
    internal partial Property<Size> Size { get; init; }
    
    [WidgetProperty]
    public partial Property<Offset> Offset { get; internal init; }

    public void ComputeDesiredSizeApplySizeCorrectionAndSetSize(BoxConstraints constraints)
    {
        ComputeAndSetDesiredSize(constraints);
        Size desiredSize = DesiredSize.Value;
        
        /*
         TODO : Add a size correction strategy system
         Size correction strategy ( For now its just clamping )
         */
        Size correctedSize
            = new(
                Math.Clamp(desiredSize.Width, constraints.MinWidth, constraints.MaxWidth),
                Math.Clamp(desiredSize.Height, constraints.MinHeight, constraints.MaxWidth)
            );
        Size.Value = correctedSize;
    }
    
    /// <summary>
    /// Uses constraints to compute and set this <see cref="Widget"/>'s <see cref="DesiredSize"/>.
    /// If this <see cref="Widget"/> has children,
    /// their <see cref="Size"/>'s need to be set by using <see cref="ComputeDesiredSizeApplySizeCorrectionAndSetSize"/>,
    /// and their <see cref="Offset"/> needs to be set as well.
    /// </summary>
    /// <param name="constraints">Constraints to consider when calculating its <see cref="DesiredSize"/>.</param>
    public abstract void ComputeAndSetDesiredSize(BoxConstraints constraints);
}