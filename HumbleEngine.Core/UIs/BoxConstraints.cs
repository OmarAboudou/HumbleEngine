namespace HumbleEngine.Core;

public readonly record struct BoxConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints)
{
    public float MinWidth => WidthConstraints.MinLength;
    public float MaxWidth => WidthConstraints.MaxLength;
    public float MinHeight => HeightConstraints.MinLength;
    public float MaxHeight => HeightConstraints.MaxLength;
}