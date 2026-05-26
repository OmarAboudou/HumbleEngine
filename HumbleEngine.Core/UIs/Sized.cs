namespace HumbleEngine.Core;

public partial record Sized : SingleChildWidget<Widget>
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<Size> DesiredSize { get; init; }
    
    protected override LayoutResult PerformLayout(BoxConstraints boxConstraints)
    {
        if (Child.Value is null)
            return new(DesiredSize.Value, new(0, 0));

        (Size desiredSize,Size minContentSize) = Child.Value.PerformLayoutClamped(boxConstraints.Tighten(DesiredSize));
        Size clampedSize = new(Math.Clamp(desiredSize.Width, minContentSize.Width, boxConstraints.MaxWidth), Math.Clamp(desiredSize.Height, minContentSize.Height, boxConstraints.MaxHeight));
        Child.Value.Size.Value = clampedSize;
        Child.Value.Offset.Value = new(0, 0);
        return new(clampedSize, minContentSize);

    }
}