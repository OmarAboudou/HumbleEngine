namespace HumbleEngine;

public readonly record struct Position(float X, float Y)
{
    public static Position Zero => new(0f, 0f);

    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
}
