namespace HumbleEngine;

public readonly struct Font
{
    public static readonly Font Default = new(16f);

    public Typeface? Typeface { get; }
    public float     Size     { get; }
    public float     ScaleX   { get; }
    public float     SkewX    { get; }

    public Font(float size, float scaleX = 1f, float skewX = 0f)
    {
        Size   = size;
        ScaleX = scaleX;
        SkewX  = skewX;
    }

    public Font(Typeface typeface, float size, float scaleX = 1f, float skewX = 0f)
        : this(size, scaleX, skewX)
    {
        Typeface = typeface;
    }
}
