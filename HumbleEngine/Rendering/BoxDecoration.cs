namespace HumbleEngine;

public readonly record struct BoxDecoration()
{
    public Color?        Color        { get; init; }
    public BorderRadius  BorderRadius { get; init; }
    public BoxBorder?    Border       { get; init; }
    public BoxShadow[]   Shadows      { get; init; } = [];
}
