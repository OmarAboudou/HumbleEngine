namespace HumbleEngine;

public readonly record struct BoxConstraints(
    LengthConstraints WidthConstraints,
    LengthConstraints HeightConstraints)
{
    public float MinWidth  => WidthConstraints.Min;
    public float MaxWidth  => WidthConstraints.Max;
    public float MinHeight => HeightConstraints.Min;
    public float MaxHeight => HeightConstraints.Max;

    // --- Factories ---

    public static BoxConstraints Tight(Size size) =>
        new(LengthConstraints.Tight(size.Width), LengthConstraints.Tight(size.Height));

    public static BoxConstraints Tight(float width, float height) =>
        new(LengthConstraints.Tight(width), LengthConstraints.Tight(height));

    public static BoxConstraints Loose(Size maxSize) =>
        new(LengthConstraints.Loose(maxSize.Width), LengthConstraints.Loose(maxSize.Height));

    public static BoxConstraints Loose(float maxWidth, float maxHeight) =>
        new(LengthConstraints.Loose(maxWidth), LengthConstraints.Loose(maxHeight));

    public static BoxConstraints Expand() =>
        new(LengthConstraints.Unbounded, LengthConstraints.Unbounded);

    // Tight sur les axes renseignés, non borné sur les autres.
    public static BoxConstraints TightFor(float? width = null, float? height = null) =>
        new(
            width  is { } w ? LengthConstraints.Tight(w) : LengthConstraints.Unbounded,
            height is { } h ? LengthConstraints.Tight(h) : LengthConstraints.Unbounded
        );

    // --- Méthodes d'instance ---

    // Clamp une taille à l'intérieur des contraintes.
    public Size Constrain(Size size) =>
        new(WidthConstraints.Constrain(size.Width), HeightConstraints.Constrain(size.Height));

    // Supprime les minima (utilisé par Column/Row pour laisser les enfants choisir leur taille).
    public BoxConstraints Loosen() =>
        new(WidthConstraints.Loosen(), HeightConstraints.Loosen());

    // Réduit les maxima d'un espace horizontal et vertical (utilisé par Padding).
    public BoxConstraints Deflate(float horizontal, float vertical) =>
        new(
            new LengthConstraints(
                Math.Max(0f, MinWidth  - horizontal),
                Math.Max(0f, MaxWidth  - horizontal)),
            new LengthConstraints(
                Math.Max(0f, MinHeight - vertical),
                Math.Max(0f, MaxHeight - vertical))
        );

    // Intersection avec un autre set de contraintes (utilisé par ConstrainedBox).
    public BoxConstraints Enforce(BoxConstraints other) =>
        new(EnforceAxis(WidthConstraints,  other.WidthConstraints),
            EnforceAxis(HeightConstraints, other.HeightConstraints));

    private static LengthConstraints EnforceAxis(LengthConstraints a, LengthConstraints b)
    {
        float min = Math.Max(a.Min, b.Min);
        float max = Math.Max(Math.Min(a.Max, b.Max), min);
        return new LengthConstraints(min, max);
    }
}
