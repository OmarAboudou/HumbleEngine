namespace HumbleEngine.Sandbox;

/// <summary>
/// A UI node that draws a fixed string by hand from a baked glyph atlas (roadmap
/// 10, bloc 3 — text before any Label): the font follows the tree lifecycle like
/// every GPU resource (atlas acquired from the context renderer in
/// <see cref="OnAttached"/>, released in <see cref="OnDetached"/>). The pen walks
/// left to right, placing each glyph bitmap by its bearing on a single baseline —
/// the simplest layout, the real one arrives at bloc 4.
/// </summary>
public sealed class TextNode : UINode
{
    private const int PixelSize = 32;
    private const string Text = "Humble Engine — éàçôû 0123";

    private GlyphAtlas? _atlas;

    /// <inheritdoc />
    protected override void OnAttached()
    {
        using var font = Font.Default(PixelSize);
        _atlas = new GlyphAtlas(Renderer!, font); // font's glyphs are now baked; the face can go
    }

    /// <inheritdoc />
    protected override void OnDetached()
    {
        _atlas?.Dispose();
        _atlas = null;
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        var origin = GlobalRect;
        var baselineY = origin.Y + PixelSize; // a rough ascent; proper line metrics are bloc 4
        var penX = origin.X;
        var color = new Vector4(0.95f, 0.95f, 0.95f, 1f);

        foreach (var character in Text)
        {
            if (!_atlas!.TryGet(character, out var glyph))
                continue;

            if (glyph.Size.X > 0f)
            {
                var rect = new Rect(
                    penX + glyph.Bearing.X, baselineY - glyph.Bearing.Y, glyph.Size.X, glyph.Size.Y);
                renderer.DrawGlyph(rect, _atlas.Texture, glyph.UvSubRect, color);
            }

            penX += glyph.Advance;
        }
    }
}
