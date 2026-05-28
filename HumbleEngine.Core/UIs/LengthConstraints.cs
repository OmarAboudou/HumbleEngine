namespace HumbleEngine.Core;

public readonly record struct LengthConstraints(float Min, float Max)
{
    public LengthConstraints Loosen()
        => this with { Min = 0 };
}