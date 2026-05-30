namespace HumbleEngine;

public readonly record struct BoxShadow(
    Color Color,
    float BlurRadius   = 0f,
    float SpreadRadius = 0f,
    float OffsetX      = 0f,
    float OffsetY      = 0f);
