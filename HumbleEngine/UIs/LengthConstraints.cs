namespace HumbleEngine;

public readonly record struct LengthConstraints(float Min, float Max)
{
    public static LengthConstraints Tight(float value)        => new(value, value);
    public static LengthConstraints Loose(float max)          => new(0f, max);
    public static LengthConstraints Unbounded                 => new(0f, float.PositiveInfinity);

    public float Constrain(float value) => Math.Clamp(value, Min, Max);
    public LengthConstraints Loosen()   => this with { Min = 0f };
}
