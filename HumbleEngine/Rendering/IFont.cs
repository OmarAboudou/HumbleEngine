namespace HumbleEngine;

public interface IFont : IDisposable
{
    ITypeface Typeface { get; }
    float     Size     { get; }
    float     ScaleX   { get; }
    float     SkewX    { get; }

    float MeasureText(string text);
}
