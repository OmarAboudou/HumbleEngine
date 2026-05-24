namespace HumbleEngine;

/// <summary>
/// Position et taille calculées d'un élément (hors marges).
/// </summary>
public readonly record struct LayoutBox(float X, float Y, float Width, float Height)
{
    public float Right  => X + Width;
    public float Bottom => Y + Height;

    public Rect ToRect() => Rect.FromXYWH(X, Y, Width, Height);
}
