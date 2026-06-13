using HumbleEngine.Tests.SceneGraph;

namespace HumbleEngine.Tests.Text;

/// <summary>
/// Unit tests for <see cref="Font"/> + <see cref="GlyphAtlas"/> — no display, no
/// GPU: FreeType rasterizes on the CPU and the atlas bakes into a
/// <see cref="FakeRenderer"/>. Asserting that 'A' comes out with sane pixel
/// metrics is also what validates the hand-mapped 64-bit FreeType struct offsets:
/// a wrong offset yields garbage dimensions (or a bad pointer), not a near-square
/// ~20×26 bitmap.
/// </summary>
public sealed class GlyphAtlasTests
{
    private const int PixelSize = 32;

    private static GlyphAtlas Bake(FakeRenderer renderer)
    {
        using var font = Font.Default(PixelSize);
        return new GlyphAtlas(renderer, font);
    }

    [Test]
    public void LetterA_HasSaneMetricsAndAtlasRect()
    {
        using var atlas = Bake(new FakeRenderer());

        Assert.That(atlas.TryGet('A', out var a), Is.True);

        // A capital at 32 px is a few tens of pixels each way — not zero, not huge.
        Assert.That(a.Size.X, Is.GreaterThan(0f).And.LessThan(PixelSize * 2f));
        Assert.That(a.Size.Y, Is.GreaterThan(0f).And.LessThan(PixelSize * 2f));
        Assert.That(a.Advance, Is.GreaterThan(0f).And.LessThan(PixelSize * 2f));
        // Top bearing is positive (the glyph sits above the baseline).
        Assert.That(a.Bearing.Y, Is.GreaterThan(0f));

        // Its UV rectangle is a real sub-region of the 0..1 atlas.
        Assert.That(a.UvSubRect.Width, Is.GreaterThan(0f));
        Assert.That(a.UvSubRect.X, Is.GreaterThanOrEqualTo(0f));
        Assert.That(a.UvSubRect.X + a.UvSubRect.Width, Is.LessThanOrEqualTo(1f));
        Assert.That(a.UvSubRect.Y + a.UvSubRect.Height, Is.LessThanOrEqualTo(1f));
    }

    [Test]
    public void Space_HasAdvanceButNoBitmap()
    {
        using var atlas = Bake(new FakeRenderer());

        Assert.That(atlas.TryGet(' ', out var space), Is.True);
        Assert.That(space.Size, Is.EqualTo(Vector2.Zero));
        Assert.That(space.UvSubRect.Width, Is.EqualTo(0f));
        Assert.That(space.Advance, Is.GreaterThan(0f));
    }

    [Test]
    public void FrenchAccent_IsInTheBakedCharset()
    {
        using var atlas = Bake(new FakeRenderer());

        // Latin-1 supplement: the accents Omar types (validated at bloc 5).
        Assert.That(atlas.TryGet('é', out var e), Is.True);
        Assert.That(e.Size.X, Is.GreaterThan(0f));
    }

    [Test]
    public void CharOutsideCharset_IsNotFound()
    {
        using var atlas = Bake(new FakeRenderer());

        // '€' (U+20AC) is past the Latin-1 supplement — never baked.
        Assert.That(atlas.TryGet('€', out _), Is.False);
    }

    [Test]
    public void Atlas_UploadsOneR8SizedTexture()
    {
        var renderer = new FakeRenderer();
        using var atlas = Bake(renderer);

        Assert.That(renderer.Textures, Has.Count.EqualTo(1));
        Assert.That(atlas.Texture.Width, Is.EqualTo(512));
        Assert.That(atlas.Texture.Height, Is.EqualTo(512));
        Assert.That(atlas.PixelSize, Is.EqualTo(PixelSize));
    }
}
