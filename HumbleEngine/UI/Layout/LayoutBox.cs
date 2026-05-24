namespace HumbleEngine;

/// <summary>
/// Position et taille calculées d'un élément (hors marges).
/// </summary>
public readonly record struct LayoutBox(float X, float Y, float Width, float Height)
{
    public float Right  => X + Width;
    public float Bottom => Y + Height;

    public bool Contains(Vector2<float> p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;

    public Rect ToRect() => Rect.FromXYWH(X, Y, Width, Height);
}
