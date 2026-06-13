namespace HumbleEngine;

/// <summary>
/// A baked glyph atlas: a fixed charset rasterized once into a single-channel
/// (<see cref="TextureFormat.R8"/>) coverage texture, with each glyph's place
/// and metrics recorded for layout. Shelf-packed, fixed size — a dynamic atlas
/// (glyphs uploaded on demand) is deferred with its client (large charsets).
/// The texture follows the usual ownership: dispose the atlas before its renderer.
/// </summary>
public sealed class GlyphAtlas : IDisposable
{
    private const int Width   = 512;
    private const int Height  = 512;
    private const int Padding = 1;

    private readonly Dictionary<char, Glyph> _glyphs = [];
    private bool _disposed;

    /// <summary>The coverage atlas texture, sampled by <see cref="IRenderer.DrawGlyph"/>.</summary>
    public ITexture Texture { get; }

    /// <summary>The font's pixel size — the natural line scale for layout.</summary>
    public int PixelSize { get; }

    /// <summary>Pixels from the baseline up to the font's top (from the baked font).</summary>
    public float Ascent { get; }

    /// <summary>Baseline-to-baseline distance in pixels — the natural line spacing.</summary>
    public float LineHeight { get; }

    /// <summary>Bakes the engine's default charset (printable ASCII + Latin-1) of <paramref name="font"/>.</summary>
    public GlyphAtlas(IRenderer renderer, Font font) : this(renderer, font, DefaultCharset())
    {
    }

    /// <summary>
    /// Bakes <paramref name="charset"/> of <paramref name="font"/>: rasterize each
    /// glyph, shelf-pack the coverage into one CPU bitmap, record its UV rect and
    /// metrics, then upload the bitmap as an immutable texture.
    /// </summary>
    /// <exception cref="InvalidOperationException">The atlas overflowed — reduce the pixel size or the charset.</exception>
    public GlyphAtlas(IRenderer renderer, Font font, IEnumerable<char> charset)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(font);
        PixelSize  = font.PixelSize;
        Ascent     = font.Ascent;
        LineHeight = font.LineHeight;

        var atlas = new byte[Width * Height];
        int penX = 0, penY = 0, rowHeight = 0;

        foreach (var character in charset)
        {
            var raster = font.Rasterize(character);

            // Blank glyph (space): no bitmap to place, just an advance.
            if (raster.Width == 0 || raster.Rows == 0)
            {
                _glyphs[character] = new Glyph(
                    character, default, Vector2.Zero,
                    new Vector2(raster.BitmapLeft, raster.BitmapTop), raster.Advance);
                continue;
            }

            if (penX + raster.Width + Padding > Width)
            {
                penX = 0;
                penY += rowHeight + Padding;
                rowHeight = 0;
            }
            if (penY + raster.Rows > Height)
                throw new InvalidOperationException(
                    "Glyph atlas overflowed — reduce the pixel size or the charset.");

            Blit(atlas, raster, penX, penY);

            _glyphs[character] = new Glyph(
                character,
                new Rect(penX / (float)Width, penY / (float)Height, raster.Width / (float)Width, raster.Rows / (float)Height),
                new Vector2(raster.Width, raster.Rows),
                new Vector2(raster.BitmapLeft, raster.BitmapTop),
                raster.Advance);

            penX += raster.Width + Padding;
            rowHeight = Math.Max(rowHeight, raster.Rows);
        }

        Texture = renderer.CreateTexture(atlas, Width, Height, TextureFormat.R8);
    }

    /// <summary>The baked glyph for <paramref name="character"/>, if it is in the charset.</summary>
    public bool TryGet(char character, out Glyph glyph) => _glyphs.TryGetValue(character, out glyph);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Texture.Dispose();
    }

    /// <summary>Copies a glyph's coverage rows into the atlas at the pen position.</summary>
    private static void Blit(byte[] atlas, RasterizedGlyph raster, int penX, int penY)
    {
        for (var r = 0; r < raster.Rows; r++)
            Array.Copy(raster.Coverage, r * raster.Width, atlas, (penY + r) * Width + penX, raster.Width);
    }

    /// <summary>Printable ASCII (32–126) then the Latin-1 supplement (160–255) — covers French accents.</summary>
    private static IEnumerable<char> DefaultCharset()
    {
        for (var c = 32; c <= 126; c++)
            yield return (char)c;
        for (var c = 160; c <= 255; c++)
            yield return (char)c;
    }
}
