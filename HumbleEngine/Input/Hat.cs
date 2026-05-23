namespace HumbleEngine;

public readonly struct Hat
{
    public int         Index    { get; }
    public HatPosition Position { get; }

    public Hat(int index, HatPosition position)
    {
        Index    = index;
        Position = position;
    }
}
