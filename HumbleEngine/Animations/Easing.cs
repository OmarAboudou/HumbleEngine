namespace HumbleEngine;

public static class Easing
{
    public static readonly Func<float, float> Linear    = t => t;
    public static readonly Func<float, float> EaseIn    = t => t * t * t;
    public static readonly Func<float, float> EaseOut   = t => 1f - MathF.Pow(1f - t, 3f);
    public static readonly Func<float, float> EaseInOut = t => t < 0.5f
        ? 4f * t * t * t
        : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
}
