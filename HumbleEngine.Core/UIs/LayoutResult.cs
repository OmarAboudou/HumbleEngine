namespace HumbleEngine.Core;

public readonly record struct LayoutResult(
    Size desiredSize, 
    Size minContentSize);