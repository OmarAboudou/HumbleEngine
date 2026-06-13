namespace HumbleEngine;

/// <summary>
/// One glyph placed by the layout: where to draw it (<see cref="LocalRect"/>,
/// pixels relative to the text's top-left corner) and where to sample it
/// (<see cref="UvSubRect"/>, the atlas rectangle) — fed straight to
/// <see cref="IRenderer.DrawGlyph"/> once offset by the node's global position.
/// </summary>
public readonly record struct PositionedGlyph(Rect LocalRect, Rect UvSubRect);

/// <summary>
/// Turns a string into placed glyphs on a single line: the pen walks left to
/// right by each glyph's advance, the bitmap sits at its bearing on a baseline
/// at <see cref="GlyphAtlas.Ascent"/> from the top. Multi-line (<c>\n</c>) and
/// word-wrap are deferred — the wrap needs a width constraint that fights
/// content sizing. Kerning is deferred too; advance-only reads clean for DejaVu.
/// </summary>
public static class TextLayout
{
    /// <summary>
    /// Places <paramref name="text"/>'s glyphs into <paramref name="output"/>
    /// (cleared first), relative to the top-left corner, and returns the line's
    /// measured size (advance width × line height). Characters outside the atlas
    /// charset are skipped.
    /// </summary>
    public static Vector2 Arrange(GlyphAtlas atlas, string text, List<PositionedGlyph> output)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(output);

        output.Clear();
        var penX = 0f;

        foreach (var character in text)
        {
            if (!atlas.TryGet(character, out var glyph))
                continue;

            if (glyph.Size.X > 0f)
            {
                var rect = new Rect(
                    penX + glyph.Bearing.X,
                    atlas.Ascent - glyph.Bearing.Y,
                    glyph.Size.X,
                    glyph.Size.Y);
                output.Add(new PositionedGlyph(rect, glyph.UvSubRect));
            }

            penX += glyph.Advance;
        }

        return new Vector2(penX, atlas.LineHeight);
    }
}
