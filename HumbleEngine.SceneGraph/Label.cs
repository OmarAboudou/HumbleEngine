namespace HumbleEngine;

/// <summary>
/// A single line of read-only text that <b>measures itself</b>: it writes its own
/// <see cref="UINode.Size"/> from the text, so a Label in a <see cref="Column"/>
/// or <see cref="Row"/> makes it restack for free (the layout already reacts to a
/// child's size). It is also <b>selectable</b> — drag or Shift+arrows to select,
/// Ctrl+C to copy — all inherited from <see cref="SelectableText"/>; it only adds
/// content sizing and draws at its top-left corner.
/// </summary>
public sealed class Label : SelectableText
{
    /// <inheritdoc />
    protected override void OnMeasured(Vector2 size) => Size.Value = size;

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        var box = GlobalRect;
        DrawHighlightAndGlyphs(renderer, box.X, box.Y);
    }
}
