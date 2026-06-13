using System.Diagnostics;

namespace HumbleEngine;

/// <summary>
/// A single-line editable text box — the first real client of two-way binding
/// (<see cref="SelectableText.Text"/> is meant to be wired to a model). A
/// fixed-size box (unlike <see cref="Label"/>, which sizes to its content): it
/// draws a background, the selection and text (from the base), and a blinking
/// caret. On top of the base's selection and copy it adds insertion, deletion
/// (by character or word), and cut/paste. Key repeat is the backend's job.
/// </summary>
public sealed class TextField : SelectableText
{
    private const float Inset = 4f;
    private const float CaretWidth = 1.5f;
    private const long BlinkHalfMs = 530;

    private readonly Stopwatch _blink = Stopwatch.StartNew();

    /// <summary>The box's fill colour.</summary>
    public Reactive<Vector4> Background { get; }

    public TextField()
    {
        Background = CreateReactive(new Vector4(0.14f, 0.14f, 0.18f, 1f));
    }

    /// <inheritdoc />
    protected override float TextOriginX => GlobalRect.X + Inset;

    /// <inheritdoc />
    protected override void OnCaretMoved() => _blink.Restart();

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        if (inputEvent is TextInput text)
        {
            Type(text.Text);
            return true;
        }
        return base.OnInput(inputEvent);
    }

    /// <inheritdoc />
    protected override bool OnKey(Key key, KeyModifiers modifiers)
    {
        var ctrl = (modifiers & KeyModifiers.Ctrl) != 0;
        switch (key)
        {
            case Key.X when ctrl: Cut(); return true;
            case Key.V when ctrl: Paste(); return true;
            case Key.Backspace: return DeleteBack(ctrl);
            case Key.Delete:    return DeleteForward(ctrl);
            default: return base.OnKey(key, modifiers); // selection, navigation, copy
        }
    }

    /// <summary>Replaces the current selection (or inserts at the caret) with <paramref name="inserted"/>.</summary>
    private void Type(string inserted)
    {
        if (string.IsNullOrEmpty(inserted))
            return;
        var text = Text.Value;
        Edit(string.Concat(text[..SelStart], inserted, text[SelEnd..]), SelStart + inserted.Length);
    }

    private bool DeleteBack(bool word)
    {
        if (HasSelection) { DeleteRange(SelStart, SelEnd); return true; }
        var start = word ? PrevWordBoundary(Caret) : Caret - 1;
        if (start >= 0 && start < Caret)
            DeleteRange(start, Caret);
        return true;
    }

    private bool DeleteForward(bool word)
    {
        if (HasSelection) { DeleteRange(SelStart, SelEnd); return true; }
        var end = word ? NextWordBoundary(Caret) : Caret + 1;
        if (end > Caret && end <= Text.Value.Length)
            DeleteRange(Caret, end);
        return true;
    }

    private void DeleteRange(int start, int end) =>
        Edit(Text.Value.Remove(start, end - start), start);

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

    /// <summary>Sets caret and anchor to <paramref name="caret"/> (a collapsed selection), then the text.</summary>
    private void Edit(string newText, int caret)
    {
        Caret = Anchor = caret;
        _blink.Restart();
        Text.Value = newText; // fires Text.Changed → re-measure, clamp
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        if (Atlas is null)
            return;

        var box = GlobalRect;
        renderer.DrawQuad(box, Background.Value);

        var textX = box.X + Inset;
        var textY = box.Y + (box.Height - Atlas.LineHeight) / 2f;
        DrawHighlightAndGlyphs(renderer, textX, textY);

        // Caret: only when focused, blinking on its own clock (reset on every action).
        if (ReferenceEquals(Tree?.FocusedNode, this) && (_blink.ElapsedMilliseconds / BlinkHalfMs) % 2 == 0)
            renderer.DrawQuad(
                new Rect(textX + AdvanceUpTo(Caret), textY, CaretWidth, Atlas.LineHeight), Color.Value);
    }
}
