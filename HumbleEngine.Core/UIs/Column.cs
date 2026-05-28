namespace HumbleEngine.Core;

public partial record Column : LinearLayout
{
    /*public override void ComputeAndSetDesiredSize(BoxConstraints constraints)
    {
        float heightAcc = 0;
        float maxChildWidth = 0;
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
            maxChildWidth = Math.Max(maxChildWidth, childNaturalSize.Width);
            float childNaturalHeight = childNaturalSize.Height;
            heightAcc += childNaturalHeight;
            if (i < Children.Count - 1) 
                heightAcc += Spacing.Value;
        }

        float width = maxChildWidth;
        float height = MainAxisSize.Value == Core.MainAxisSize.MAX ? constraints.MaxHeight : heightAcc;
        DesiredSize.Value = new(width, height);
    }*/

    protected override (float mainAxisSize, float crossAxisSize) ExtractMainAndCrossAxisSize(Size size)
        => (size.Height, size.Width);

    protected override float ComputeMainAxisMaxSize(BoxConstraints constraints)
        => constraints.MaxHeight;

    protected override Size ComputeDesiredSizeFromAxisSizes(float mainAxisSize, float crossAxisSize)
        => new(crossAxisSize, mainAxisSize);

    protected override Offset ComputeOffsetFromMainCrossAxisCoordinates(float mainAxisCoord, float crossAxisCoord)
        => new(crossAxisCoord, mainAxisCoord);
}