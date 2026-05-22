using SkiaSharp;

namespace HumbleEngine;

public readonly struct SpanData
{
    public string  Content  { get; init; }
    public SKColor Color    { get; init; }
    public float   FontSize { get; init; }
}
