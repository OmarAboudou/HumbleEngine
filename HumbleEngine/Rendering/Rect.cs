namespace HumbleEngine;

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public float Right  => X + Width;
    public float Bottom => Y + Height;

    public static Rect FromPositionAndSize(Position pos, Size size) =>
        new(pos.X, pos.Y, size.Width, size.Height);

    public Rect Translate(float dx, float dy) => new(X + dx, Y + dy, Width, Height);
    public Rect Inflate(float horizontal, float vertical) =>
        new(X - horizontal / 2f, Y - vertical / 2f, Width + horizontal, Height + vertical);
}
