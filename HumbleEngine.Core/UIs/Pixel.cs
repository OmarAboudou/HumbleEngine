namespace HumbleEngine.Core;

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

public static class PixelExtensions
{
    public static Pixel Px(this int pixels) => new(pixels);
    public static Pixel Px(this float pixels) => new(pixels);

}