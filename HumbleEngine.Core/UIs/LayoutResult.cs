namespace HumbleEngine.Core;

public readonly record struct LayoutResult(
    float Width,
    float Height,
    float MinContentWidth,
    float MinContentHeight);