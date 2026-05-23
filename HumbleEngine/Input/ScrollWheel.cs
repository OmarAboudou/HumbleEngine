namespace HumbleEngine;

public readonly struct ScrollWheel(float x, float y)
{
    public float X { get; } = x;
    public float Y { get; } = y;
}
