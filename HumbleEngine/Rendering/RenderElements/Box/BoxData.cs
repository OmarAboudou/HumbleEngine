using SkiaSharp;

namespace HumbleEngine;

public readonly struct BoxData
{
    public SKColor BackgroundColor { get; init; }
    public float   CornerRadius    { get; init; }
    public SKColor BorderColor     { get; init; }
    public float   BorderWidth     { get; init; }
}
