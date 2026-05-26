namespace HumbleEngine.Core;

public readonly record struct LengthConstraints(float MaxLength);

public abstract record Length
{
    public abstract float Compute(LengthConstraints constraints);
    
    public static implicit operator Length(float value)
        => new Pixel(value);
}

public record Pixel : Length
{
    internal Pixel(float Pixels)
    {
        this.Pixels = Pixels;
    }

    public float Pixels { get; init; }

    public override float Compute(LengthConstraints constraints)
        => Pixels;
    
}

public record Percent(float Percentage) : Length
{
    public override float Compute(LengthConstraints constraints)
    {
        return (Percentage/100f) * constraints.MaxLength;
    }
    
    public static implicit operator Percent(float percentage)
        => new(percentage);
}

public static class LengthExtensions
{
    public static Pixel Px(this int pixels) => new(pixels);
    public static Pixel Px(this float pixels) => new(pixels);
    
    public static Percent Percent(this int percent) => new(percent);
    public static Percent Percent(this float percent) => new(percent);

}
