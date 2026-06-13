namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Label"/> and the tree's shared font (no display —
/// the atlas bakes into a <see cref="FakeRenderer"/>): a label gets its atlas
/// from the tree, measures itself from its text, and — because the layout reacts
/// to a child's size — makes its container restack with no extra wiring.
/// </summary>
public sealed class LabelTests
{
    [Test]
    public void DefaultFontAtlas_IsLazyAndShared()
    {
        var renderer = new FakeRenderer();
        using var tree = new SceneTree(renderer);

        Assert.That(renderer.Textures, Is.Empty); // nothing baked until asked

        var first  = tree.DefaultFontAtlas;
        var second = tree.DefaultFontAtlas;

        Assert.That(first, Is.SameAs(second));
        Assert.That(renderer.Textures, Has.Count.EqualTo(1));
    }

    [Test]
    public void Label_MeasuresItself_FromText()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var label = new Label();
        tree.Root = label; // attaches: acquires the atlas, measures (empty so far)

        label.Text.Value = "Hello";

        Assert.That(label.Size.Value.X, Is.GreaterThan(0f));
        Assert.That(label.Size.Value.Y, Is.EqualTo(tree.DefaultFontAtlas.LineHeight));
    }

    [Test]
    public void Label_EmptyText_HasZeroWidthButLineHeight()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var label = new Label();
        tree.Root = label;

        Assert.That(label.Size.Value.X, Is.EqualTo(0f));
        Assert.That(label.Size.Value.Y, Is.EqualTo(tree.DefaultFontAtlas.LineHeight));
    }

    [Test]
    public void Label_IsSelectable_AndCopiesToTheClipboard()
    {
        var clipboard = new FakeClipboard();
        using var tree = new SceneTree(new FakeRenderer()) { Clipboard = clipboard };
        var label = new Label();
        tree.Root = label;
        label.Text.Value = "hello";

        // A read-only label still selects (Ctrl+A) and copies (Ctrl+C) — the shared
        // SelectableText machinery, no editing.
        label.GrabFocus();
        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.Ctrl));
        tree.RouteInput(new KeyPressed(Key.C, KeyModifiers.Ctrl));

        Assert.That(clipboard.Text, Is.EqualTo("hello"));
        Assert.That(label.Text.Value, Is.EqualTo("hello")); // unchanged — read-only
    }

    [Test]
    public void Label_ContentSizing_RestacksItsRow()
    {
        using var tree = new SceneTree(new FakeRenderer());
        var row   = new Row();
        var label = new Label();
        var marker = new Panel();
        marker.Size.Value = new Vector2(40f, 20f);
        row.Children.Add(label);
        row.Children.Add(marker);
        tree.Root = row; // empty label has zero width → marker sits at x = 0

        var before = marker.Position.Value.X;
        label.Text.Value = "WWWW"; // wider → the Row restacks → marker slides right
        var after = marker.Position.Value.X;

        Assert.That(after, Is.GreaterThan(before));
    }
}
