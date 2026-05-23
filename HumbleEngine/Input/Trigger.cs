namespace HumbleEngine;

public readonly struct Trigger
{
    public int   Index    { get; }
    public float Position { get; }

    public Trigger(int index, float position)
    {
        Index    = index;
        Position = position;
    }
}
