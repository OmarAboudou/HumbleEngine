namespace HumbleEngine.Core;

public record Percent(float Percentage) : Length
{
    public override float Compute(LengthConstraints constraints)
    {
        return Percentage/100f * constraints.MaxLength;
    }
    
    public static implicit operator Percent(float percentage)
        => new(percentage);
}

public static class PercentExtensions
{
    public static Percent Percent(this int percent) => new(percent);
    public static Percent Percent(this float percent) => new(percent);
}