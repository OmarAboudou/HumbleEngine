namespace HumbleEngine.Core;

public readonly record struct LengthConstraints(float MinLength, float MaxLength)
{
    public LengthConstraints Loosen()
        => this with { MinLength = 0 };

    public LengthConstraints Biggest()
        => this with { MinLength = MaxLength };

    public LengthConstraints Tighten(float Value)
        => new(
            MinLength: Math.Clamp(Value, MinLength, MaxLength),
            MaxLength: Math.Clamp(Value, MinLength, MaxLength)
            );

}