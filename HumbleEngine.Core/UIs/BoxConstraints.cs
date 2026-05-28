namespace HumbleEngine.Core;

public readonly record struct BoxConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints)
{
    public BoxConstraints(
        float minWidth,
        float maxWidth,
        float minHeight,
        float maxHeight)
        : this(
            new LengthConstraints(minWidth, maxWidth),
            new LengthConstraints(minHeight, maxHeight))
    { }
    
    public float MinWidth => WidthConstraints.Min;
    public float MaxWidth => WidthConstraints.Max;
    public float MinHeight => HeightConstraints.Min;
    public float MaxHeight => HeightConstraints.Max;

    public BoxConstraints Loosen()
        => this.LoosenWidth().LoosenHeight();

    public BoxConstraints LoosenWidth()
        => this with { WidthConstraints = WidthConstraints.Loosen() };
    
    public BoxConstraints LoosenHeight()
        => this with { HeightConstraints = HeightConstraints.Loosen() };
}