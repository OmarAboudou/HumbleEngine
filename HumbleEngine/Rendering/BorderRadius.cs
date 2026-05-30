namespace HumbleEngine;

public readonly record struct BorderRadius(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public static BorderRadius Zero                    => new(0f, 0f, 0f, 0f);
    public static BorderRadius All(float r)            => new(r, r, r, r);
    public static BorderRadius Circular(float r)       => All(r);
    public static BorderRadius Horizontal(float r)     => new(r, r, 0f, 0f);
    public static BorderRadius Only(
        float topLeft     = 0f, float topRight    = 0f,
        float bottomRight = 0f, float bottomLeft  = 0f)
        => new(topLeft, topRight, bottomRight, bottomLeft);
}
