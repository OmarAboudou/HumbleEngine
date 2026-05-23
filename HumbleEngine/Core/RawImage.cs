namespace HumbleEngine;

public readonly struct RawImage(int width, int height, Memory<byte> pixels)
{
    public int          Width  { get; } = width;
    public int          Height { get; } = height;
    public Memory<byte> Pixels { get; } = pixels;
}
