namespace HumbleEngine;

public readonly struct BoxConstraints
{
    public float MinWidth  { get; init; }
    public float MaxWidth  { get; init; }
    public float MinHeight { get; init; }
    public float MaxHeight { get; init; }

    // Pas de minimum, maximum = taille disponible — le child choisit sa taille
    public static BoxConstraints Loose(Size available) => new()
    {
        MinWidth  = 0, MaxWidth  = available.Width,
        MinHeight = 0, MaxHeight = available.Height
    };

    // Le child doit faire exactement cette taille
    public static BoxConstraints Tight(Size size) => new()
    {
        MinWidth  = size.Width,  MaxWidth  = size.Width,
        MinHeight = size.Height, MaxHeight = size.Height
    };

    // Aucune contrainte — le child peut faire la taille qu'il veut
    public static BoxConstraints Unconstrained => new()
    {
        MinWidth  = 0, MaxWidth  = float.PositiveInfinity,
        MinHeight = 0, MaxHeight = float.PositiveInfinity
    };

    // Clamp une taille candidate dans les contraintes
    public Size Constrain(float width, float height) => new(
        Math.Clamp(width,  MinWidth,  MaxWidth),
        Math.Clamp(height, MinHeight, MaxHeight)
    );
}
