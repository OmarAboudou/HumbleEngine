namespace HumbleEngine.Core;

public partial record Row : LinearLayout
{
    protected override (float mainAxisSize, float crossAxisSize) ExtractMainAndCrossAxisSize(Size size)
        => (size.Width, size.Height);

    protected override float ComputeMainAxisMaxSize(BoxConstraints constraints)
        => constraints.MaxWidth;

    protected override Size ComputeDesiredSizeFromAxisSizes(float mainAxisSize, float crossAxisSize)
        => new(mainAxisSize, crossAxisSize);

    protected override Position ComputeOffsetFromMainCrossAxisCoordinates(float mainAxisCoord, float crossAxisCoord)
        => new(mainAxisCoord, crossAxisCoord);
}