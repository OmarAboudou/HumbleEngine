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
    {
        
    }
}