namespace HumbleEngine.Core;

public readonly record struct LengthConstraints(float MinLength, float MaxLength);

public abstract record Length
{
    public abstract float Compute(LengthConstraints constraints);
    
    public static implicit operator Length(float value)
        => new Pixel(value);
}
