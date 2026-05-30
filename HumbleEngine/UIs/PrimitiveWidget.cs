namespace HumbleEngine;

public abstract partial record PrimitiveWidget : Widget
{
    [WidgetProperty]
    public partial Property<Size> DesiredSize { get; internal init; }

    [WidgetProperty]
    internal partial Property<Size> Size { get; init; }

    [WidgetProperty]
    public partial Property<Position> LocalPosition { get; internal init; }

    [WidgetProperty]
    public partial Property<Position> ViewportPosition { get; internal init; }
    
    public abstract Size ComputeAndSetDesiredSize(BoxConstraints constraints);
}