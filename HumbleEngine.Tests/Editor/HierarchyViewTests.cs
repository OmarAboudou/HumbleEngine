using HumbleEngine.Tests.SceneGraph;

namespace HumbleEngine.Tests.Editor;

/// <summary>
/// Unit tests for <see cref="HierarchyView"/> / <see cref="HierarchyRow"/>: the panel
/// mirrors the real tree (a row per node), recurses, updates live, and — on click —
/// selects the node into the <see cref="EditorState"/> with a highlight following the
/// selection.
/// </summary>
public sealed class HierarchyViewTests
{
    private static List<HierarchyRow> TopRows(HierarchyView view) =>
        ((Column)view.Children.Single()).Children.Cast<HierarchyRow>().ToList();

    private static List<HierarchyRow> SubRows(HierarchyRow row) =>
        row.Children.OfType<HierarchyRow>().ToList();

    private static string LabelOf(HierarchyRow row) =>
        row.Children.OfType<Label>().Single().Text.Value;

    private static Panel BackgroundOf(HierarchyRow row) =>
        row.Children.OfType<Panel>().Single();

    /// <summary>
    /// Wraps <paramref name="view"/> in a <see cref="Column"/> root that provides
    /// <paramref name="editor"/>, then attaches both to a live tree so that
    /// <see cref="Node.OnAttached"/> fires (rows inherit the <see cref="EditorState"/>).
    /// The returned tree must be disposed by the caller.
    /// </summary>
    private static SceneTree InEditorTree(UINode view, EditorState editor)
    {
        var root = new Column();
        root.Provide(editor);
        var tree = new SceneTree(new FakeRenderer());
        tree.Root = root;
        root.Children.Add(view);
        return tree;
    }

    // ── Structure ────────────────────────────────────────────────────────────

    [Test]
    public void TopRows_MirrorRootChildren_InOrder()
    {
        var root = new Column();
        var a = new Panel { Name = "A" };
        var b = new Panel { Name = "B" };
        root.Children.Add(a);
        root.Children.Add(b);

        var view = new HierarchyView(root);

        Assert.That(TopRows(view).Select(r => r.Node), Is.EqualTo(new Node[] { a, b }));
    }

    [Test]
    public void Rows_RecurseIntoChildren()
    {
        var root = new Column();
        var parent = new Column { Name = "P" };
        var child = new Panel { Name = "C" };
        parent.Children.Add(child);
        root.Children.Add(parent);

        var view = new HierarchyView(root);

        var parentRow = TopRows(view).Single();
        Assert.That(parentRow.Node, Is.SameAs(parent));
        Assert.That(SubRows(parentRow).Select(r => r.Node), Is.EqualTo(new Node[] { child }));
    }

    [Test]
    public void Rows_UpdateLive_WhenTheTreeChanges()
    {
        var root = new Column();
        var view = new HierarchyView(root);
        Assert.That(TopRows(view), Is.Empty);

        var a = new Panel();
        root.Children.Add(a);
        Assert.That(TopRows(view).Select(r => r.Node), Is.EqualTo(new Node[] { a }));

        root.Children.Remove(a);
        Assert.That(TopRows(view), Is.Empty);
    }

    [Test]
    public void Row_LabelText_IsNameWhenPresent_ElseType()
    {
        Assert.That(LabelOf(new HierarchyRow(new Panel { Name = "Hero" })), Is.EqualTo("Hero"));
        Assert.That(LabelOf(new HierarchyRow(new Panel())), Is.EqualTo("Panel"));
    }

    // ── Selection / input (require live tree + EditorState context) ──────────

    [Test]
    public void ClickingARow_SelectsItsNode()
    {
        var root = new Column();
        var a = new Panel { Name = "Hero" };
        root.Children.Add(a);
        var view = new HierarchyView(root);
        var editor = new EditorState();
        using var tree = InEditorTree(view, editor);

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(2f, 2f)));

        Assert.That(editor.Selection.Value, Is.SameAs(a));
    }

    [Test]
    public void Row_Background_HighlightsWhenItsNodeIsSelected()
    {
        var root = new Column();
        var a = new Panel { Name = "A" };
        root.Children.Add(a);
        var view = new HierarchyView(root);
        var editor = new EditorState();
        using var tree = InEditorTree(view, editor);

        var background = BackgroundOf(TopRows(view).Single());

        Assert.That(background.Color.Value.W, Is.EqualTo(0f)); // transparent: not selected

        editor.Selection.Value = a;
        Assert.That(background.Color.Value.W, Is.GreaterThan(0f)); // highlighted

        editor.Selection.Value = null;
        Assert.That(background.Color.Value.W, Is.EqualTo(0f)); // back to transparent
    }
}
