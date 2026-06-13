namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="TextField"/> — no display: a <see cref="SceneTree"/>
/// over a <see cref="FakeRenderer"/>, synthetic input routed in. Covers editing,
/// caret navigation, click-to-place, focus gating and the two-way binding that is
/// the field's reason to exist.
/// </summary>
public sealed class TextFieldTests
{
    private static (SceneTree Tree, TextField Field) Focused()
    {
        var tree = new SceneTree(new FakeRenderer()) { Clipboard = new FakeClipboard() };
        var field = new TextField();
        field.Size.Value = new Vector2(200f, 30f);
        tree.Root = field;
        field.GrabFocus();
        return (tree, field);
    }

    [Test]
    public void TextInput_InsertsAtTheCaret()
    {
        var (tree, field) = Focused();
        using var _ = tree;

        tree.RouteInput(new TextInput("H"));
        tree.RouteInput(new TextInput("i"));

        Assert.That(field.Text.Value, Is.EqualTo("Hi"));
    }

    [Test]
    public void Backspace_DeletesBeforeTheCaret()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("Hi"));

        tree.RouteInput(new KeyPressed(Key.Backspace, KeyModifiers.None));

        Assert.That(field.Text.Value, Is.EqualTo("H"));
    }

    [Test]
    public void HomeThenInsert_WritesAtTheStart()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("abc"));

        tree.RouteInput(new KeyPressed(Key.Home, KeyModifiers.None));
        tree.RouteInput(new TextInput("X"));

        Assert.That(field.Text.Value, Is.EqualTo("Xabc"));
    }

    [Test]
    public void LeftThenDelete_RemovesAtTheCaret()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("abc"));

        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.None)); // caret between b and c
        tree.RouteInput(new KeyPressed(Key.Delete, KeyModifiers.None));

        Assert.That(field.Text.Value, Is.EqualTo("ab"));
    }

    [Test]
    public void Keyboard_DoesNothing_WhenNotFocused()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var field = new TextField { };
        field.Size.Value = new Vector2(200f, 30f);
        tree.Root = field; // never focused — keyboard routes to the focused node, which is null

        tree.RouteInput(new TextInput("ignored"));

        Assert.That(field.Text.Value, Is.Empty);
    }

    [Test]
    public void Click_FocusesAndPlacesTheCaret()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var field = new TextField();
        field.Size.Value = new Vector2(200f, 30f);
        tree.Root = field;
        field.Text.Value = "Hello";

        // Click past the end → caret at the end; typing appends.
        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(190f, 15f)));
        Assert.That(tree.FocusedNode, Is.SameAs(field));
        tree.RouteInput(new TextInput("!"));
        Assert.That(field.Text.Value, Is.EqualTo("Hello!"));

        // Click at the far left → caret at 0; typing prepends.
        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(0f, 15f)));
        tree.RouteInput(new TextInput("X"));
        Assert.That(field.Text.Value, Is.EqualTo("XHello!"));
    }

    [Test]
    public void TwoWayBinding_PropagatesBothWays()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        var model = new Reactive<string>(string.Empty);
        field.Text.BindTwoWayFrom(model);

        // Editing the field reaches the model.
        tree.RouteInput(new TextInput("Hi"));
        Assert.That(model.Value, Is.EqualTo("Hi"));

        // Writing the model reaches the field.
        model.Value = "Bye";
        Assert.That(field.Text.Value, Is.EqualTo("Bye"));
    }

    // --- Selection (bloc 5b) ---

    [Test]
    public void ShiftArrow_Selects_AndTypingReplaces()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("hello")); // caret at the end

        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.Shift));
        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.Shift)); // "lo" selected
        tree.RouteInput(new TextInput("X"));

        Assert.That(field.Text.Value, Is.EqualTo("helX"));
    }

    [Test]
    public void Backspace_DeletesTheSelection_NotJustOneChar()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("hello"));
        tree.RouteInput(new KeyPressed(Key.Home, KeyModifiers.None));
        tree.RouteInput(new KeyPressed(Key.Right, KeyModifiers.Shift));
        tree.RouteInput(new KeyPressed(Key.Right, KeyModifiers.Shift)); // "he" selected

        tree.RouteInput(new KeyPressed(Key.Backspace, KeyModifiers.None));

        Assert.That(field.Text.Value, Is.EqualTo("llo"));
    }

    [Test]
    public void CtrlA_SelectsAll_AndTypingReplacesAll()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("hello"));

        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.Ctrl));
        tree.RouteInput(new TextInput("Z"));

        Assert.That(field.Text.Value, Is.EqualTo("Z"));
    }

    [Test]
    public void Drag_SelectsARange()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var field = new TextField();
        field.Size.Value = new Vector2(200f, 30f);
        tree.Root = field;
        field.Text.Value = "Hello";

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(0f, 15f)));   // caret 0, drag begins
        tree.RouteInput(new PointerMoved(new Vector2(190f, 15f)));                       // captured: extends to the end
        tree.RouteInput(new TextInput("Z"));                                            // replaces the whole selection

        Assert.That(field.Text.Value, Is.EqualTo("Z"));
    }

    [Test]
    public void DoubleClick_SelectsTheWord()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var field = new TextField();
        field.Size.Value = new Vector2(200f, 30f);
        tree.Root = field;
        field.Text.Value = "foo bar";

        var atLeft = new Vector2(0f, 15f); // index 0, inside "foo"
        tree.RouteInput(new PointerPressed(PointerButton.Left, atLeft));
        tree.RouteInput(new PointerPressed(PointerButton.Left, atLeft)); // second click → word select
        tree.RouteInput(new TextInput("X"));

        Assert.That(field.Text.Value, Is.EqualTo("X bar"));
    }

    // --- Word operations (bloc 5b) ---

    [Test]
    public void CtrlBackspace_DeletesTheWordBeforeTheCaret()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("foo bar")); // caret at the end

        tree.RouteInput(new KeyPressed(Key.Backspace, KeyModifiers.Ctrl));

        Assert.That(field.Text.Value, Is.EqualTo("foo "));
    }

    [Test]
    public void CtrlLeft_MovesByWord()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("foo bar")); // caret 7

        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.Ctrl)); // caret to start of "bar" (4)
        tree.RouteInput(new TextInput("X"));

        Assert.That(field.Text.Value, Is.EqualTo("foo Xbar"));
    }

    // --- Clipboard (bloc 6) ---

    [Test]
    public void CtrlC_CopiesTheSelection_ToTheClipboard()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("hello"));
        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.Shift));
        tree.RouteInput(new KeyPressed(Key.Left, KeyModifiers.Shift)); // "lo" selected

        tree.RouteInput(new KeyPressed(Key.C, KeyModifiers.Ctrl));

        Assert.That(((FakeClipboard)tree.Clipboard!).Text, Is.EqualTo("lo"));
        Assert.That(field.Text.Value, Is.EqualTo("hello")); // copy leaves the text
    }

    [Test]
    public void CtrlX_CutsTheSelection()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.RouteInput(new TextInput("hello"));
        tree.RouteInput(new KeyPressed(Key.Home, KeyModifiers.None));
        tree.RouteInput(new KeyPressed(Key.Right, KeyModifiers.Shift));
        tree.RouteInput(new KeyPressed(Key.Right, KeyModifiers.Shift)); // "he" selected

        tree.RouteInput(new KeyPressed(Key.X, KeyModifiers.Ctrl));

        Assert.That(((FakeClipboard)tree.Clipboard!).Text, Is.EqualTo("he"));
        Assert.That(field.Text.Value, Is.EqualTo("llo"));
    }

    [Test]
    public void CtrlV_PastesAtTheCaret_OverTheSelection()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.Clipboard!.SetText("ABC");
        tree.RouteInput(new TextInput("hello"));
        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.Ctrl)); // select all

        tree.RouteInput(new KeyPressed(Key.V, KeyModifiers.Ctrl));

        Assert.That(field.Text.Value, Is.EqualTo("ABC"));
    }

    [Test]
    public void Paste_FlattensNewlines_OnOneLine()
    {
        var (tree, field) = Focused();
        using var _ = tree;
        tree.Clipboard!.SetText("a\r\nb");

        tree.RouteInput(new KeyPressed(Key.V, KeyModifiers.Ctrl));

        Assert.That(field.Text.Value, Is.EqualTo("a b"));
    }
}
