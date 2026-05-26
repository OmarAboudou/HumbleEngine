namespace HumbleEngine.Core;

public abstract partial record Widget : HumbleRecord
{
    protected Property<T> CreatePublicProperty<T>(T initialValue, WidgetRefreshFlag flag = WidgetRefreshFlag.NONE)
    {
        Property<T> property = base.CreatePublicProperty(initialValue);
        return ConnectFlag(flag, property);
    }

    protected Property<T> ConnectFlag<T>(WidgetRefreshFlag flag, Property<T> property)
    {
        if (flag.HasFlag(WidgetRefreshFlag.PAINT))
        {
            property.Connect( _ => Flag.Value &= WidgetRefreshFlag.PAINT );
        }
        if (flag.HasFlag(WidgetRefreshFlag.LAYOUT))
        {
            property.Connect( _ => Flag.Value &= WidgetRefreshFlag.LAYOUT );
        }
        return property;
    }
    
    protected ListProperty<T> CreatePublicListProperty<T>(IReadOnlyList<T>? initialElements = null, WidgetRefreshFlag flag = WidgetRefreshFlag.NONE)
    {
        ListProperty<T> property = base.CreatePublicListProperty(initialElements);
        return ConnectFlag(flag, property);
    }

    protected ListProperty<T> ConnectFlag<T>(WidgetRefreshFlag flag, ListProperty<T> property)
    {
        if (flag.HasFlag(WidgetRefreshFlag.PAINT))
        {
            property.ConnectAddedElement( (_,_) => Flag.Value &= WidgetRefreshFlag.PAINT );
            property.ConnectRemovedElement( (_,_) => Flag.Value &= WidgetRefreshFlag.PAINT );
        }
        if (flag.HasFlag(WidgetRefreshFlag.LAYOUT))
        {
            property.ConnectAddedElement( (_,_) => Flag.Value &= WidgetRefreshFlag.LAYOUT );
            property.ConnectRemovedElement( (_,_) => Flag.Value &= WidgetRefreshFlag.LAYOUT );
        }
        return property;
    }
    
 
    public object? Key { get ; init; }
    
    [WidgetProperty]
    internal partial Property<WidgetRefreshFlag> Flag { get; init; }

    [WidgetProperty]
    internal partial Property<Size> Size { get; init; }
    
    [WidgetProperty]
    internal partial Property<Size> MinContentSize { get; init; }
     
    [WidgetProperty]
    internal partial Property<Offset> Offset { get; init; }
    
    public void Layout(BoxConstraints boxConstraints)
    {
        (Size desiredSize, Size minContentSize) = PerformLayoutClamped(boxConstraints);
        
        Size.Value = desiredSize;
        MinContentSize.Value = minContentSize;
    }

    public LayoutResult PerformLayoutClamped(BoxConstraints boxConstraints)
    {
        ((float width, float height), (float minContentWidth, float minContentHeight)) = PerformLayout(boxConstraints);
        
        if (width > boxConstraints.MinWidth || height > boxConstraints.MinHeight)
        {
            Console.WriteLine($"Size {(width, height)} returned by {this} is beyond constraints {boxConstraints} and will be clamped");
        }
        width = Math.Clamp(width, boxConstraints.MinWidth, boxConstraints.MaxWidth);
        height = Math.Clamp(height, boxConstraints.MinHeight, boxConstraints.MaxHeight);

        if (minContentWidth > width || minContentHeight > height)
        {
            Console.WriteLine($"Min content size {(minContentWidth, minContentHeight)} returned by {this} exceeded size {(width, height)} and will be clamped");
        }
        minContentWidth = Math.Clamp(minContentWidth, 0, width);
        minContentHeight = Math.Clamp(minContentHeight, 0, height);

        return new(new(width, height), new(minContentWidth, minContentHeight));
    }
    
    /// <summary>
    /// Takes some constraints to respect and returns the desired size
    /// and the minimum content size which is the smallest size that can fit its content.
    /// Inside this method, you should also set the <see cref="Size"/> and <see cref="Offset"/>
    /// properties of any child widget.
    /// </summary>
    /// <param name="boxConstraints">The constraints enforced onto me.</param>
    /// <returns>This <see cref="Widget"/>'s desired size and minimum content size</returns>
    protected abstract LayoutResult PerformLayout(BoxConstraints boxConstraints);
}



    