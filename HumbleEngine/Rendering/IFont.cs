namespace HumbleEngine;

public interface IFont
{
    ITypeface Typeface { get; }
    float     Size     { get; }
    float     ScaleX   { get; }
    float     SkewX    { get; }

    float MeasureText(string text);
}
