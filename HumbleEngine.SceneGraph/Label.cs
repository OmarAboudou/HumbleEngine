namespace HumbleEngine;

/// <summary>
/// A single line of text. Reads its glyphs from the tree's shared
/// <see cref="SceneTree.DefaultFontAtlas"/> (acquired on attach, like every node
/// gets its renderer from the tree), lays them out, and <b>measures itself</b>:
/// writing its own <see cref="UINode.Size"/> from the text. That is content
/// sizing — and because the layout containers already react to a child's
/// <see cref="UINode.Size"/> changing, a Label in a <see cref="Column"/> or
/// <see cref="Row"/> makes it restack for free, no extra wiring.
/// <para>
/// <see cref="Text"/> drives the re-measure (it changes the geometry);
/// <see cref="Color"/> is read at draw time, so tinting never relayouts.
/// </para>
/// </summary>
public sealed class Label : UINode
{
    private readonly List<PositionedGlyph> _glyphs = [];
    private GlyphAtlas? _atlas;

    /// <summary>The text to render — changing it re-measures the label.</summary>
    public Reactive<string> Text { get; }

    /// <summary>The glyph colour, read every frame (no relayout on change).</summary>
    public Reactive<Vector4> Color { get; }

    public Label()
    {
        Text  = CreateReactive(string.Empty);
        Color = CreateReactive(new Vector4(1f, 1f, 1f, 1f));
        Text.Changed += _ => Measure();
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        _atlas = Tree!.DefaultFontAtlas;
        Measure();
    }

    /// <inheritdoc />
    protected override void OnDetached()
    {
        _atlas = null;
        _glyphs.Clear();
    }

    /// <summary>Re-lays out the glyphs and writes the measured size — the parent layout reacts.</summary>
    private void Measure()
    {
        if (_atlas is null)
            return;
        Size.Value = TextLayout.Arrange(_atlas, Text.Value, _glyphs);
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        if (_atlas is null)
            return;

        var origin = GlobalRect;
        var color = Color.Value;
        foreach (var glyph in _glyphs)
            renderer.DrawGlyph(
                new Rect(origin.X + glyph.LocalRect.X, origin.Y + glyph.LocalRect.Y,
                         glyph.LocalRect.Width, glyph.LocalRect.Height),
                _atlas.Texture, glyph.UvSubRect, color);
    }
}
