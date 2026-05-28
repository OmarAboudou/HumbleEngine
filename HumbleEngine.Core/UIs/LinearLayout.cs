namespace HumbleEngine.Core;

public abstract partial record LinearLayout : MultipleChildrenWidget<Widget>
{
    [WidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<MainAxisAlignment> MainAxisAlignment { get; init; }
    
    [WidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<CrossAxisAlignment> CrossAxisAlignment { get; init; }
    
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<MainAxisSize> MainAxisSize { get; init; }

    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<float> Spacing { get; init; }

    public override void ComputeAndSetDesiredSize(BoxConstraints constraints)
    {
        List<(float mainAxisSize, float crossAxisSize)> childNaturalSizes = [];
        float mainAxisAcc = 0;
        float maxChildCrossAxisSize = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            Widget child = Children[i];
            child.ComputeDesiredSizeApplySizeCorrectionAndSetSize(
                new BoxConstraints(
                    new(
                        0,
                        float.PositiveInfinity
                    ),
                    new(
                        0,
                        float.PositiveInfinity
                    )
                )
            );
            Size childNaturalSize = child.DesiredSize.Value;
            (float mainAxisSize, float crossAxisSize)  = ExtractMainAndCrossAxisSize(childNaturalSize);
            maxChildCrossAxisSize = Math.Max(maxChildCrossAxisSize, crossAxisSize);
            mainAxisAcc += mainAxisSize;
            
            childNaturalSizes.Add((mainAxisAcc, mainAxisSize));
            
            if (i < Children.Count - 1) 
                mainAxisAcc += Spacing.Value;
            
        }

        float finalMainAxisSize
            = MainAxisSize.Value == Core.MainAxisSize.MAX
                ? Math.Clamp(mainAxisAcc, 0, ComputeMainAxisMaxSize(constraints))
                : mainAxisAcc;
        
        DesiredSize.Value = ComputeDesiredSizeFromAxisSizes(finalMainAxisSize, maxChildCrossAxisSize);

        for (int i = 0; i < Children.Count; i++)
        {
            Widget child = Children[i];
            float spacingsSize = Math.Max(0, i - 1) * Spacing.Value;
            float mainAxisSize = 0;
            for (int j = 0; j < i; j++)
                mainAxisSize += childNaturalSizes[j].mainAxisSize;
            
            child.Offset.Value 
                = ComputeOffsetFromMainCrossAxisCoordinates(
                    mainAxisSize + spacingsSize,
                    childNaturalSizes[i].crossAxisSize);
        }
    }
    
    protected abstract (float mainAxisSize, float crossAxisSize) ExtractMainAndCrossAxisSize(Size size);

    protected abstract float ComputeMainAxisMaxSize(BoxConstraints constraints);
    protected abstract Size ComputeDesiredSizeFromAxisSizes(
        float mainAxisSize,
        float crossAxisSize);

    protected abstract Offset ComputeOffsetFromMainCrossAxisCoordinates(
        float mainAxisCoord,
        float crossAxisCoord);

}