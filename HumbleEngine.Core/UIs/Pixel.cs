namespace HumbleEngine.Core;

public record Pixel(float Pixels) : Length
{
    public override float Compute(LengthConstraints constraints)
        => Pixels;
}