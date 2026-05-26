namespace HumbleEngine.Core;

public readonly record struct LayoutConstraints(
    float MinWidth, 
    float MinHeight, 
    float MaxWidth, 
    float MaxHeight
);