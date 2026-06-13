namespace HumbleEngine;

/// <summary>
/// A baked glyph: where it lives in the atlas and how to place it on a line.
/// All the layout machinery (bloc 4) needs and nothing it does not.
/// </summary>
/// <param name="Character">The character this glyph renders.</param>
/// <param name="UvSubRect">
/// The glyph's rectangle in the atlas, normalized 0..1 — fed straight to
/// <see cref="IRenderer.DrawGlyph"/>. Empty for a blank glyph (e.g. space).
/// </param>
/// <param name="Size">The glyph bitmap's size in pixels (zero for a blank glyph).</param>
/// <param name="Bearing">
/// Pixels from the pen to the bitmap's top-left: X right to the left edge, Y up
/// from the baseline to the top edge — the offset that places the bitmap.
/// </param>
/// <param name="Advance">How far the pen moves after this glyph, in pixels.</param>
public readonly record struct Glyph(
    char Character,
    Rect UvSubRect,
    Vector2 Size,
    Vector2 Bearing,
    float Advance);
