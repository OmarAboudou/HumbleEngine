using System.Diagnostics;

namespace HumbleEngine;

/// <summary>
/// A single-line editable text box — the first real client of two-way binding
/// (<see cref="Text"/> is a <see cref="Reactive{T}"/> meant to be wired to a
/// model). A fixed-size box (unlike <see cref="Label"/>, which sizes to its
/// content): it draws a background, the selection highlight, the text, and a
/// blinking caret, and edits when focused.
/// <para>
/// Carries a <b>selection</b> (an anchor and the caret — equal means none):
/// Shift+arrows and mouse drag extend it, typing or deletion replaces it,
/// Ctrl+A takes all. Ctrl moves and deletes by <b>word</b>. Key repeat is the
/// backend's job (the field is blind to it). Clipboard, undo and overflow
/// scrolling are deferred.
/// </para>
/// </summary>
public sealed class TextField : UINode
{
    private const float Inset = 4f;
    private const float CaretWidth = 1.5f;
    private const long BlinkHalfMs = 530;
    private const long DoubleClickMs = 350;

    private readonly List<PositionedGlyph> _glyphs = [];
    private readonly Stopwatch _blink = Stopwatch.StartNew();
    private GlyphAtlas? _atlas;
    private int _caret;
    private int _anchor;
    private bool _dragging;
    private long _lastClickTick = long.MinValue / 2;
    private int _lastClickIndex;

    /// <summary>The edited text — wire it to a model with <see cref="Reactive{T}.BindTwoWayFrom(Reactive{T})"/>.</summary>
    public Reactive<string> Text { get; }

    /// <summary>Glyph and caret colour.</summary>
    public Reactive<Vector4> Color { get; }

    /// <summary>The box's fill colour.</summary>
    public Reactive<Vector4> Background { get; }

    /// <summary>The selection highlight colour, drawn behind the selected glyphs.</summary>
    public Reactive<Vector4> Selection { get; }

    public TextField()
    {
        Text       = CreateReactive(string.Empty);
        Color      = CreateReactive(new Vector4(0.95f, 0.95f, 0.95f, 1f));
        Background = CreateReactive(new Vector4(0.14f, 0.14f, 0.18f, 1f));
        Selection  = CreateReactive(new Vector4(0.25f, 0.45f, 0.9f, 0.6f));
        Text.Changed += _ => OnTextChanged();
    }

    private bool HasSelection => _anchor != _caret;
    private int SelStart => Math.Min(_anchor, _caret);
    private int SelEnd => Math.Max(_anchor, _caret);

    /// <inheritdoc />
    protected override void OnAttached()
    {
        _atlas = Tree!.DefaultFontAtlas;
        Relayout();
    }

    /// <inheritdoc />
    protected override void OnDetached()
    {
        _atlas = null;
        _glyphs.Clear();
    }

    /// <summary>External writes (a binding) can land any text — keep caret and anchor in range.</summary>
    private void OnTextChanged()
    {
        var length = Text.Value.Length;
        _caret  = Math.Clamp(_caret, 0, length);
        _anchor = Math.Clamp(_anchor, 0, length);
        Relayout();
    }

    private void Relayout()
    {
        if (_atlas is not null)
            TextLayout.Arrange(_atlas, Text.Value, _glyphs);
    }

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

            case TextInput text:
                Type(text.Text);
                return true;

            case KeyPressed key:
                return HandleKey(key.Key, key.Modifiers);

            default:
                return false;
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
            _anchor = start;
            _caret = end;
        }
        else
        {
            _caret = _anchor = index;
            _dragging = true;
        }

        _lastClickTick = now;
        _lastClickIndex = index;
        _blink.Restart();
    }

    private bool HandleKey(Key key, KeyModifiers modifiers)
    {
        var ctrl  = (modifiers & KeyModifiers.Ctrl) != 0;
        var shift = (modifiers & KeyModifiers.Shift) != 0;
        var text  = Text.Value;

        switch (key)
        {
            case Key.A when ctrl:
                _anchor = 0;
                _caret = text.Length;
                _blink.Restart();
                return true;

            case Key.C when ctrl: Copy(); return true;
            case Key.X when ctrl: Cut(); return true;
            case Key.V when ctrl: Paste(); return true;

            case Key.Backspace: return DeleteBack(ctrl);
            case Key.Delete:    return DeleteForward(ctrl);

            case Key.Left:
                if (ctrl) SetCaret(PrevWordBoundary(_caret), shift);
                else if (HasSelection && !shift) SetCaret(SelStart, extend: false);
                else SetCaret(_caret - 1, shift);
                return true;

            case Key.Right:
                if (ctrl) SetCaret(NextWordBoundary(_caret), shift);
                else if (HasSelection && !shift) SetCaret(SelEnd, extend: false);
                else SetCaret(_caret + 1, shift);
                return true;

            case Key.Home: SetCaret(0, shift); return true;
            case Key.End:  SetCaret(text.Length, shift); return true;

            default: return false; // Enter, Tab, Ctrl+C… are not ours — let them bubble
        }
    }

    private bool DeleteBack(bool word)
    {
        if (HasSelection) { DeleteRange(SelStart, SelEnd); return true; }
        var start = word ? PrevWordBoundary(_caret) : _caret - 1;
        if (start >= 0 && start < _caret)
            DeleteRange(start, _caret);
        return true;
    }

    private bool DeleteForward(bool word)
    {
        if (HasSelection) { DeleteRange(SelStart, SelEnd); return true; }
        var end = word ? NextWordBoundary(_caret) : _caret + 1;
        if (end > _caret && end <= Text.Value.Length)
            DeleteRange(_caret, end);
        return true;
    }

    /// <summary>The selected substring (empty when there is no selection).</summary>
    private string SelectedText => Text.Value[SelStart..SelEnd];

    /// <summary>Copies the selection to the tree's clipboard.</summary>
    private void Copy()
    {
        if (HasSelection)
            Tree?.Clipboard?.SetText(SelectedText);
    }

    /// <summary>Copies the selection to the clipboard, then deletes it.</summary>
    private void Cut()
    {
        if (!HasSelection)
            return;
        Tree?.Clipboard?.SetText(SelectedText);
        DeleteRange(SelStart, SelEnd);
    }

    /// <summary>Pastes the clipboard text (newlines flattened — this is one line) over the selection or at the caret.</summary>
    private void Paste()
    {
        var text = Tree?.Clipboard?.GetText();
        if (string.IsNullOrEmpty(text))
            return;
        Type(text.Replace("\r", string.Empty).Replace('\n', ' '));
    }

    /// <summary>Replaces the current selection (or inserts at the caret) with <paramref name="inserted"/>.</summary>
    private void Type(string inserted)
    {
        if (string.IsNullOrEmpty(inserted))
            return;
        var text = Text.Value;
        Edit(string.Concat(text[..SelStart], inserted, text[SelEnd..]), SelStart + inserted.Length);
    }

    private void DeleteRange(int start, int end) =>
        Edit(Text.Value.Remove(start, end - start), start);

    /// <summary>Sets caret and anchor to <paramref name="caret"/> (a collapsed selection), then the text.</summary>
    private void Edit(string newText, int caret)
    {
        _caret = _anchor = caret;
        _blink.Restart();
        Text.Value = newText;
    }

    /// <summary>Moves the caret; <paramref name="extend"/> keeps the anchor (growing the selection), else collapses it.</summary>
    private void SetCaret(int caret, bool extend)
    {
        _caret = Math.Clamp(caret, 0, Text.Value.Length);
        if (!extend)
            _anchor = _caret;
        _blink.Restart();
    }

    /// <summary>Start of the word reached by skipping whitespace then a run of word characters, leftward.</summary>
    private int PrevWordBoundary(int index)
    {
        var text = Text.Value;
        var i = index;
        while (i > 0 && char.IsWhiteSpace(text[i - 1])) i--;
        while (i > 0 && !char.IsWhiteSpace(text[i - 1])) i--;
        return i;
    }

    /// <summary>End of the word reached by skipping whitespace then a run of word characters, rightward.</summary>
    private int NextWordBoundary(int index)
    {
        var text = Text.Value;
        var i = index;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
        return i;
    }

    /// <summary>The word (run of non-whitespace) containing or adjacent to <paramref name="index"/>.</summary>
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

    /// <summary>Nearest caret index to a window-space X — the boundary closest to the click.</summary>
    private int CaretIndexAt(float globalX)
    {
        if (_atlas is null)
            return _caret;

        var localX = globalX - GlobalRect.X - Inset;
        if (localX <= 0f)
            return 0;

        var text = Text.Value;
        var advance = 0f;
        for (var i = 0; i < text.Length; i++)
        {
            if (!_atlas.TryGet(text[i], out var glyph))
                continue;
            if (localX < advance + glyph.Advance / 2f)
                return i;
            advance += glyph.Advance;
        }
        return text.Length;
    }

    /// <summary>Sum of advances of the first <paramref name="count"/> characters — an X offset into the line.</summary>
    private float AdvanceUpTo(int count)
    {
        if (_atlas is null)
            return 0f;

        var text = Text.Value;
        var advance = 0f;
        for (var i = 0; i < count && i < text.Length; i++)
        {
            if (_atlas.TryGet(text[i], out var glyph))
                advance += glyph.Advance;
        }
        return advance;
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        if (_atlas is null)
            return;

        var box = GlobalRect;
        renderer.DrawQuad(box, Background.Value);

        var textX = box.X + Inset;
        var textY = box.Y + (box.Height - _atlas.LineHeight) / 2f;

        // Selection highlight, behind the glyphs.
        if (HasSelection)
        {
            var start = textX + AdvanceUpTo(SelStart);
            var end = textX + AdvanceUpTo(SelEnd);
            renderer.DrawQuad(new Rect(start, textY, end - start, _atlas.LineHeight), Selection.Value);
        }

        var color = Color.Value;
        foreach (var glyph in _glyphs)
            renderer.DrawGlyph(
                new Rect(textX + glyph.LocalRect.X, textY + glyph.LocalRect.Y,
                         glyph.LocalRect.Width, glyph.LocalRect.Height),
                _atlas.Texture, glyph.UvSubRect, color);

        // Caret: only when focused, blinking on its own clock (reset on every action).
        if (ReferenceEquals(Tree?.FocusedNode, this) && (_blink.ElapsedMilliseconds / BlinkHalfMs) % 2 == 0)
            renderer.DrawQuad(
                new Rect(textX + AdvanceUpTo(_caret), textY, CaretWidth, _atlas.LineHeight), color);
    }
}
