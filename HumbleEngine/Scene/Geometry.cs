namespace HumbleEngine;

public readonly record struct Size(float Width, float Height);

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) =>
        x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
