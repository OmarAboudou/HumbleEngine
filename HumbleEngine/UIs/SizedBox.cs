namespace HumbleEngine;

public partial record SizedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Width { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Height { get; init; }

    public override void ComputeAndSetDesiredSize(BoxConstraints constraints)
    {
    }
}