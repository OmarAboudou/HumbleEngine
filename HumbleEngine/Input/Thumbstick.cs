namespace HumbleEngine;

public readonly struct Thumbstick
{
    public int   Index     { get; }
    public float X         { get; }
    public float Y         { get; }
    public float Position  => MathF.Sqrt(X * X + Y * Y);
    public float Direction => MathF.Atan2(Y, X);

    public Thumbstick(int index, float x, float y)
    {
        Index = index;
        X     = x;
        Y     = y;
    }
}
