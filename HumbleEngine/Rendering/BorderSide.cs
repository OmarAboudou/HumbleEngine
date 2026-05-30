namespace HumbleEngine;

public readonly record struct BorderSide(Color Color, float Width = 1f)
{
    public static BorderSide None => new(Color.Transparent, 0f);
}
