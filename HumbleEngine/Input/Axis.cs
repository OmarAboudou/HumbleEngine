namespace HumbleEngine;

public readonly struct Axis
{
    public int   Index    { get; }
    public float Position { get; }

    public Axis(int index, float position)
    {
        Index    = index;
        Position = position;
    }
}
