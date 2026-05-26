namespace HumbleEngine.Core;

public readonly record struct Length
{
    internal float Value { get; init; }
    internal LengthKind Kind { get; init; }

    public static implicit operator Length(float value)
        => value.Px();

    public float Compute(float relativeLength)
    {
        return Kind switch
        {
            LengthKind.PIXEL => Value,
            LengthKind.PERCENTAGE => (Value / 100f) * relativeLength,
            _ => throw new ArgumentOutOfRangeException(nameof(Kind))
        };
        
    }
}

public enum LengthKind
{
    PIXEL,
    PERCENTAGE
}

public static class LengthExtensions
{
    public static Length Px(this float value) => new() { Value = value, Kind = LengthKind.PIXEL };
    public static Length Pct(this float value) => new() { Value = value, Kind = LengthKind.PERCENTAGE };
}