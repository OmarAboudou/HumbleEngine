using System.Diagnostics;

namespace HumbleEngine;

/// <summary>
/// A single-line editable text box — the first real client of two-way binding
/// (<see cref="Text"/> is a <see cref="Reactive{T}"/> meant to be wired to a
/// model). A fixed-size box (unlike <see cref="Label"/>, which sizes to its
/// content): it draws a background, the text, and a blinking caret, and edits
/// when focused. Focus is taken on click, which also places the caret.
/// <para>
/// Key repeat is the backend's job (the field is blind to it — it just sees
/// repeated events). Selection, multi-line, IME and overflow scrolling are
/// deferred.
/// </para>
/// </summary>
public sealed class TextField : UINode
{
    private const float Inset = 4f;
    private const float CaretWidth = 1.5f;
    private static readonly long BlinkHalfMs = 530;

    private readonly List<PositionedGlyph> _glyphs = [];
    private readonly Stopwatch _blink = Stopwatch.StartNew();
    private GlyphAtlas? _atlas;
    private int _caret;

    /// <summary>The edited text — wire it to a model with <see cref="Reactive{T}.BindTwoWayFrom(Reactive{T})"/>.</summary>
    public Reactive<string> Text { get; }

    /// <summary>Glyph and caret colour.</summary>
    public Reactive<Vector4> Color { get; }

    /// <summary>The box's fill colour.</summary>
    public Reactive<Vector4> Background { get; }

    public TextField()
    {
        Text       = CreateReactive(string.Empty);
        Color      = CreateReactive(new Vector4(0.95f, 0.95f, 0.95f, 1f));
        Background = CreateReactive(new Vector4(0.14f, 0.14f, 0.18f, 1f));
        Text.Changed += _ => OnTextChanged();
    }

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

    /// <summary>External writes (a binding) can land any text — keep the caret in range.</summary>
    private void OnTextChanged()
    {
        _caret = Math.Clamp(_caret, 0, Text.Value.Length);
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
                GrabFocus();
                _caret = CaretIndexAt(pressed.Position.X);
                _blink.Restart();
                return true;

            case TextInput text:
                Insert(text.Text);
                return true;

            case KeyPressed key:
                return HandleKey(key.Key);

            default:
                return false;
        }
    }

    private bool HandleKey(Key key)
    {
        var text = Text.Value;
        switch (key)
        {
            case Key.Backspace when _caret > 0: Edit(text.Remove(_caret - 1, 1), _caret - 1); return true;
            case Key.Delete when _caret < text.Length: Edit(text.Remove(_caret, 1), _caret); return true;
            case Key.Left:  Move(_caret - 1); return true;
            case Key.Right: Move(_caret + 1); return true;
            case Key.Home:  Move(0); return true;
            case Key.End:   Move(text.Length); return true;
            default: return false; // Enter, Tab… are not ours — let them bubble
        }
    }

    private void Insert(string inserted)
    {
        if (!string.IsNullOrEmpty(inserted))
            Edit(Text.Value.Insert(_caret, inserted), _caret + inserted.Length);
    }

    /// <summary>Sets the caret first (always valid for the new text), then the text — the change relayouts.</summary>
    private void Edit(string newText, int newCaret)
    {
        _caret = newCaret;
        _blink.Restart();
        Text.Value = newText;
    }

    private void Move(int caret)
    {
        _caret = Math.Clamp(caret, 0, Text.Value.Length);
        _blink.Restart();
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

    /// <summary>Sum of advances of the first <paramref name="count"/> characters — the caret's X.</summary>
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

        var color = Color.Value;
        var textX = box.X + Inset;
        var textY = box.Y + (box.Height - _atlas.LineHeight) / 2f;

        foreach (var glyph in _glyphs)
            renderer.DrawGlyph(
                new Rect(textX + glyph.LocalRect.X, textY + glyph.LocalRect.Y,
                         glyph.LocalRect.Width, glyph.LocalRect.Height),
                _atlas.Texture, glyph.UvSubRect, color);

        // Caret: only when focused, blinking on its own clock (reset on every edit).
        if (ReferenceEquals(Tree?.FocusedNode, this) && (_blink.ElapsedMilliseconds / BlinkHalfMs) % 2 == 0)
            renderer.DrawQuad(
                new Rect(textX + AdvanceUpTo(_caret), textY, CaretWidth, _atlas.LineHeight), color);
    }
}
