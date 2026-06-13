namespace HumbleEngine;

/// <summary>
/// Shared base of <see cref="Label"/> and <see cref="TextField"/>: a line of text
/// with a <b>selection</b> (an anchor and the caret — equal means none), drawn
/// from the tree's <see cref="SceneTree.DefaultFontAtlas"/> through one shared
/// <see cref="TextLayout"/>. It owns the selection geometry — hit-testing,
/// Shift+arrows, mouse drag, double-click, Ctrl+A — and the copy, so a label and
/// a field share one implementation rather than the field composing a label
/// (caret and selection reason over a single laid-out line, they want to be one
/// object — the Godot Label/LineEdit split over a shared text server).
/// <para>
/// A read-only <see cref="Label"/> uses exactly this; an editable
/// <see cref="TextField"/> adds insertion, deletion, cut/paste and a caret on top.
/// </para>
/// </summary>
public abstract class SelectableText : UINode
{
    private const long DoubleClickMs = 350;

    /// <summary>The placed glyphs of the current text — laid out on <see cref="Remeasure"/>.</summary>
    protected readonly List<PositionedGlyph> Glyphs = [];

    /// <summary>The tree's shared atlas, acquired on attach; null while detached.</summary>
    protected GlyphAtlas? Atlas;

    /// <summary>The caret index — the moving edge of the selection.</summary>
    protected int Caret;

    /// <summary>The fixed edge of the selection (equal to <see cref="Caret"/> means none).</summary>
    protected int Anchor;

    private bool _dragging;
    private long _lastClickTick = long.MinValue / 2;
    private int _lastClickIndex;

    /// <summary>The text — changing it re-lays out the glyphs and clamps the selection.</summary>
    public Property<string> Text { get; }

    /// <summary>The glyph colour.</summary>
    public Property<Vector4> Color { get; }

    /// <summary>The selection highlight colour, drawn behind the selected glyphs.</summary>
    public Property<Vector4> Selection { get; }

    protected SelectableText()
    {
        Text      = CreateProperty(string.Empty);
        Color     = CreateProperty(new Vector4(0.95f, 0.95f, 0.95f, 1f));
        Selection = CreateProperty(new Vector4(0.25f, 0.45f, 0.9f, 0.6f));
        Text.Changed += _ => OnTextChanged();
    }

    /// <summary>Whether a non-empty range is selected.</summary>
    protected bool HasSelection => Anchor != Caret;

    /// <summary>The selection's lower index.</summary>
    protected int SelStart => Math.Min(Anchor, Caret);

    /// <summary>The selection's upper index.</summary>
    protected int SelEnd => Math.Max(Anchor, Caret);

    /// <summary>The selected substring (empty when there is no selection).</summary>
    protected string SelectedText => Text.Value[SelStart..SelEnd];

    /// <inheritdoc />
    protected override void OnAttached()
    {
        Atlas = Tree!.DefaultFontAtlas;
        Remeasure();
    }

    /// <inheritdoc />
    protected override void OnDetached()
    {
        Atlas = null;
        Glyphs.Clear();
    }

    /// <summary>External writes (a binding) can land any text — keep caret and anchor in range, relay out.</summary>
    private void OnTextChanged()
    {
        var length = Text.Value.Length;
        Caret  = Math.Clamp(Caret, 0, length);
        Anchor = Math.Clamp(Anchor, 0, length);
        Remeasure();
    }

    /// <summary>Re-lays out the glyphs and reports the measured size to subclasses.</summary>
    protected void Remeasure()
    {
        if (Atlas is null)
            return;
        var size = TextLayout.Arrange(Atlas, Text.Value, Glyphs);
        OnMeasured(size);
    }

    /// <summary>Hook for content sizing: <see cref="Label"/> writes its <see cref="UINode.Size"/>; a field ignores it.</summary>
    protected virtual void OnMeasured(Vector2 size) { }

    /// <summary>Hook fired whenever the caret or selection moves — a field restarts its caret blink.</summary>
    protected virtual void OnCaretMoved() { }

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case PointerPressed { Button: PointerButton.Left } pressed:
                OnPress(pressed.Position.X);
                return true;

            case PointerMoved moved when _dragging:
                SetCaret(CaretIndexAt(moved.Position.X), extend: true);
                return true;

            case PointerReleased { Button: PointerButton.Left }:
                _dragging = false;
                return true;

            case KeyPressed key:
                return OnKey(key.Key, key.Modifiers);

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles the selection and navigation keys (Shift+arrows, Ctrl+arrows,
    /// Home/End, Ctrl+A, Ctrl+C). A field overrides this to add its editing keys,
    /// delegating the rest here with <c>base.OnKey</c>.
    /// </summary>
    protected virtual bool OnKey(Key key, KeyModifiers modifiers)
    {
        var ctrl  = (modifiers & KeyModifiers.Ctrl) != 0;
        var shift = (modifiers & KeyModifiers.Shift) != 0;
        var text  = Text.Value;

        switch (key)
        {
            case Key.A when ctrl:
                Anchor = 0;
                Caret = text.Length;
                OnCaretMoved();
                return true;

            case Key.C when ctrl:
                Copy();
                return true;

            case Key.Left:
                if (ctrl) SetCaret(PrevWordBoundary(Caret), shift);
                else if (HasSelection && !shift) SetCaret(SelStart, extend: false);
                else SetCaret(Caret - 1, shift);
                return true;

            case Key.Right:
                if (ctrl) SetCaret(NextWordBoundary(Caret), shift);
                else if (HasSelection && !shift) SetCaret(SelEnd, extend: false);
                else SetCaret(Caret + 1, shift);
                return true;

            case Key.Home: SetCaret(0, shift); return true;
            case Key.End:  SetCaret(text.Length, shift); return true;

            default: return false;
        }
    }

    private void OnPress(float globalX)
    {
        GrabFocus();
        var index = CaretIndexAt(globalX);

        // A second click near the same spot selects the word under it.
        var now = Environment.TickCount64;
        if (now - _lastClickTick < DoubleClickMs && Math.Abs(index - _lastClickIndex) <= 1)
        {
            var (start, end) = WordAt(index);
            Anchor = start;
            Caret = end;
        }
        else
        {
            Caret = Anchor = index;
            _dragging = true;
        }

        _lastClickTick = now;
        _lastClickIndex = index;
        OnCaretMoved();
    }

    /// <summary>Moves the caret; <paramref name="extend"/> keeps the anchor (growing the selection), else collapses it.</summary>
    protected void SetCaret(int caret, bool extend)
    {
        Caret = Math.Clamp(caret, 0, Text.Value.Length);
        if (!extend)
            Anchor = Caret;
        OnCaretMoved();
    }

    /// <summary>Copies the selection to the tree's clipboard.</summary>
    protected void Copy()
    {
        if (HasSelection)
            Tree?.Clipboard?.SetText(SelectedText);
    }

    /// <summary>Start of the word reached by skipping whitespace then a run of word characters, leftward.</summary>
    protected int PrevWordBoundary(int index)
    {
        var text = Text.Value;
        var i = index;
        while (i > 0 && char.IsWhiteSpace(text[i - 1])) i--;
        while (i > 0 && !char.IsWhiteSpace(text[i - 1])) i--;
        return i;
    }

    /// <summary>End of the word reached by skipping whitespace then a run of word characters, rightward.</summary>
    protected int NextWordBoundary(int index)
    {
        var text = Text.Value;
        var i = index;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
        return i;
    }

    /// <summary>The word (run of non-whitespace) containing <paramref name="index"/>.</summary>
    private (int Start, int End) WordAt(int index)
    {
        var text = Text.Value;
        if (text.Length == 0)
            return (0, 0);
        var start = Math.Min(index, text.Length);
        var end = start;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
        return (start, end);
    }

    /// <summary>Nearest caret index to a window-space X (text origin at <paramref name="textX"/>).</summary>
    protected int CaretIndexAt(float globalX) => CaretIndexAt(globalX, TextOriginX);

    private int CaretIndexAt(float globalX, float textX)
    {
        if (Atlas is null)
            return Caret;

        var localX = globalX - textX;
        if (localX <= 0f)
            return 0;

        var text = Text.Value;
        var advance = 0f;
        for (var i = 0; i < text.Length; i++)
        {
            if (!Atlas.TryGet(text[i], out var glyph))
                continue;
            if (localX < advance + glyph.Advance / 2f)
                return i;
            advance += glyph.Advance;
        }
        return text.Length;
    }

    /// <summary>Sum of advances of the first <paramref name="count"/> characters — an X offset into the line.</summary>
    protected float AdvanceUpTo(int count)
    {
        if (Atlas is null)
            return 0f;

        var text = Text.Value;
        var advance = 0f;
        for (var i = 0; i < count && i < text.Length; i++)
        {
            if (Atlas.TryGet(text[i], out var glyph))
                advance += glyph.Advance;
        }
        return advance;
    }

    /// <summary>Window-space X where the text starts — a field insets it; a label starts at its left edge.</summary>
    protected virtual float TextOriginX => GlobalRect.X;

    /// <summary>Draws the selection highlight then the glyphs, with the text's top-left at the given window-space point.</summary>
    protected void DrawHighlightAndGlyphs(IRenderer renderer, float originX, float originY)
    {
        if (Atlas is null)
            return;

        if (HasSelection)
        {
            var start = originX + AdvanceUpTo(SelStart);
            var end = originX + AdvanceUpTo(SelEnd);
            renderer.DrawQuad(new Rect(start, originY, end - start, Atlas.LineHeight), Selection.Value);
        }

        var color = Color.Value;
        foreach (var glyph in Glyphs)
            renderer.DrawGlyph(
                new Rect(originX + glyph.LocalRect.X, originY + glyph.LocalRect.Y,
                         glyph.LocalRect.Width, glyph.LocalRect.Height),
                Atlas.Texture, glyph.UvSubRect, color);
    }
}
