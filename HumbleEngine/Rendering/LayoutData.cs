namespace HumbleEngine;

public readonly struct LayoutData
{
    public float? Width    { get; init; }  // null = hug content
    public float? Height   { get; init; }  // null = hug content
    public float  PaddingX { get; init; }
    public float  PaddingY { get; init; }
}
