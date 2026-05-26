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
        => LoosenWidth().LoosenHeight();
    public BoxConstraints LoosenWidth()
        => this with { WidthConstraints = WidthConstraints.Loosen() };
    
    public BoxConstraints LoosenHeight()
        => this with { HeightConstraints = HeightConstraints.Loosen() };

    public BoxConstraints Biggest()
        => BiggestWidth().BiggestHeight();
    
    public BoxConstraints BiggestWidth()
        => this with { WidthConstraints = WidthConstraints.Biggest() };
    
    public BoxConstraints BiggestHeight()
        => this with { HeightConstraints = HeightConstraints.Biggest() };

    public BoxConstraints Tighten(float width, float height)
        => TightenWidth(width).TightenHeight(height);

    public BoxConstraints Tighten(Size size)
        => Tighten(size.Width, size.Height);

    public BoxConstraints TightenWidth(float value)
        => this with { WidthConstraints = WidthConstraints.Tighten(value)};
    public BoxConstraints TightenHeight(float value)
        => this with { HeightConstraints = HeightConstraints.Tighten(value)};

}