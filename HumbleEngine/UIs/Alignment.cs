namespace HumbleEngine;

public readonly record struct Alignment(float X, float Y)
{
    public static Alignment TopLeft     => new(-1f, -1f);
    public static Alignment TopCenter   => new( 0f, -1f);
    public static Alignment TopRight    => new( 1f, -1f);
    public static Alignment CenterLeft  => new(-1f,  0f);
    public static Alignment Center      => new( 0f,  0f);
    public static Alignment CenterRight => new( 1f,  0f);
    public static Alignment BottomLeft  => new(-1f,  1f);
    public static Alignment BottomCenter=> new( 0f,  1f);
    public static Alignment BottomRight => new( 1f,  1f);
}
