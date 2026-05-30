namespace HumbleEngine.Core;

public readonly record struct BoxConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints)
{
    public float MinWidth 
        => WidthConstraints.Min;
    
    public float MaxWidth
        => WidthConstraints.Max;
    
    public float MinHeight
        => HeightConstraints.Min;
    
    public float MaxHeight 
        => HeightConstraints.Max;
}