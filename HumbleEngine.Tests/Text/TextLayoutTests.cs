using HumbleEngine.Tests.SceneGraph;

namespace HumbleEngine.Tests.Text;

/// <summary>
/// Unit tests for the line metrics and <see cref="TextLayout"/> — no display, the
/// atlas bakes into a <see cref="FakeRenderer"/>. The sane Ascent/LineHeight also
/// validate the second batch of hand-mapped FreeType offsets (face->size->metrics).
/// </summary>
public sealed class TextLayoutTests
{
    private const int PixelSize = 32;

    private static GlyphAtlas Bake(FakeRenderer renderer)
    {
        using var font = Font.Default(PixelSize);
        return new GlyphAtlas(renderer, font);
    }

    [Test]
    public void LineMetrics_AreSane()
    {
        using var atlas = Bake(new FakeRenderer());

        Assert.That(atlas.Ascent, Is.GreaterThan(0f).And.LessThan(PixelSize * 2f));
        Assert.That(atlas.LineHeight, Is.GreaterThan(atlas.Ascent));
        Assert.That(atlas.LineHeight, Is.LessThan(PixelSize * 2f));
    }

    [Test]
    public void Arrange_EmptyString_IsLineHeightTallAndZeroWide()
    {
        using var atlas = Bake(new FakeRenderer());
        var output = new List<PositionedGlyph>();

        var size = TextLayout.Arrange(atlas, "", output);

        Assert.That(output, Is.Empty);
        Assert.That(size.X, Is.EqualTo(0f));
        Assert.That(size.Y, Is.EqualTo(atlas.LineHeight));
    }

    [Test]
    public void Arrange_PlacesVisibleGlyphsLeftToRight()
    {
        using var atlas = Bake(new FakeRenderer());
        var output = new List<PositionedGlyph>();

        TextLayout.Arrange(atlas, "AB", output);

        Assert.That(output, Has.Count.EqualTo(2));
        Assert.That(output[1].LocalRect.X, Is.GreaterThan(output[0].LocalRect.X));
    }

    [Test]
    public void Arrange_WidthGrowsWithText()
    {
        using var atlas = Bake(new FakeRenderer());
        var output = new List<PositionedGlyph>();

        var one = TextLayout.Arrange(atlas, "A", output).X;
        var two = TextLayout.Arrange(atlas, "AA", output).X;

        Assert.That(two, Is.GreaterThan(one));
    }

    [Test]
    public void Arrange_Spaces_AdvanceWithoutGlyphs()
    {
        using var atlas = Bake(new FakeRenderer());
        var output = new List<PositionedGlyph>();

        var size = TextLayout.Arrange(atlas, "  ", output);

        Assert.That(output, Is.Empty);
        Assert.That(size.X, Is.GreaterThan(0f));
    }
}
