namespace HumbleEngine.Core;

public readonly record struct BoxConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints)
{
    public float MinWidth => WidthConstraints.MinLength;
    public float MaxWidth => WidthConstraints.MaxLength;
    public float MinHeight => HeightConstraints.MinLength;
    public float MaxHeight => HeightConstraints.MaxLength;

    public BoxConstraints Loosen()
        => this.LoosenWidth().LoosenHeight();
    public BoxConstraints LoosenWidth()
        => this with{WidthConstraints = WidthConstraints.Loosen()};
    
    public BoxConstraints LoosenHeight()
        => this with{HeightConstraints = HeightConstraints.Loosen()};

}