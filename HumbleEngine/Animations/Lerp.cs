namespace HumbleEngine;

public static class Lerp
{
    public static float Float(float a, float b, float t)
        => a + (b - a) * t;

    public static Color Color(Color a, Color b, float t)
        => new((byte)(a.R + (b.R - a.R) * t),
               (byte)(a.G + (b.G - a.G) * t),
               (byte)(a.B + (b.B - a.B) * t),
               (byte)(a.A + (b.A - a.A) * t));

    public static Vector2<float> Vector2(Vector2<float> a, Vector2<float> b, float t)
        => new(Float(a.X, b.X, t), Float(a.Y, b.Y, t));
}
