namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Column"/> and <see cref="Row"/>: stacking order
/// and spacing, and the reactive relayout on each of its three observables —
/// the children list (including departures behind the list's back), a child's
/// size, and the spacing itself. Layout runs once the container is in a tree
/// (the per-node protocol wires on attach); an unconstrained tree leaves children
/// at their content size, so the stacking is exactly the content layout.
/// </summary>
public sealed class LinearContainerTests
{
    private static Panel MakeTile(float width, float height)
    {
        var tile = new Panel();
        tile.Size.Value = new Vector2(width, height);
        return tile;
    }

    // Puts the container in a tree (no surface size → unconstrained → content layout).
    private static SceneTree Host(Node root) => new(new FakeRenderer()) { Root = root };

    [Test]
    public void Column_StacksChildren_TopToBottom_WithSpacing()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(100f, 30f);
        var c = MakeTile(100f, 50f);
        var column = new Column { Children = { a, b, c } };
        column.Spacing.Value = 10f;
        using var tree = Host(column);

        Assert.That(a.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 70f)));
        Assert.That(c.Position.Value, Is.EqualTo(new Vector2(0f, 110f)));
    }

    [Test]
    public void Column_MeasuresItself_FromItsChildren()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(80f, 30f);
        var column = new Column { Children = { a, b } };
        column.Spacing.Value = 10f;
        using var tree = Host(column);

        // Width = widest child (100); height = 60 + 10 + 30 = 100.
        Assert.That(column.Size.Value, Is.EqualTo(new Vector2(100f, 100f)));
    }

    [Test]
    public void Row_StacksChildren_LeftToRight_WithSpacing()
    {
        var a = MakeTile(60f, 100f);
        var b = MakeTile(30f, 100f);
        var row = new Row { Children = { a, b } };
        row.Spacing.Value = 5f;
        using var tree = Host(row);

        Assert.That(a.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(65f, 0f)));
    }

    [Test]
    public void ChildSizeChange_RestacksTheOnesBelow()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(100f, 30f);
        var column = new Column { Children = { a, b } };
        using var tree = Host(column);

        a.Size.Value = new Vector2(100f, 100f);

        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 100f)));
    }

    [Test]
    public void SpacingChange_Restacks()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(100f, 30f);
        var column = new Column { Children = { a, b } };
        using var tree = Host(column);

        column.Spacing.Value = 25f;

        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 85f)));
    }

    [Test]
    public void Remove_Restacks_AndStopsListeningToTheRemovedChild()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(100f, 30f);
        var column = new Column { Children = { a, b } };
        using var tree = Host(column);

        column.Children.Remove(a);
        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));

        // The departed child's size is nobody's business anymore.
        a.Size.Value = new Vector2(100f, 500f);
        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));
    }

    [Test]
    public void ChildDisposed_BehindTheListsBack_Restacks()
    {
        var a = MakeTile(100f, 60f);
        var b = MakeTile(100f, 30f);
        var column = new Column { Children = { a, b } };
        using var tree = Host(column);

        // Not removed through the list: the departure broadcast narrates anyway.
        a.Dispose();

        Assert.That(column.Children.Count, Is.EqualTo(1));
        Assert.That(b.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));
    }

    [Test]
    public void NestedContainers_ResolveGlobalRectsThroughBoth()
    {
        var tile = MakeTile(100f, 30f);
        var inner = new Row { Children = { MakeTile(40f, 30f), tile } };
        inner.Spacing.Value = 10f;
        var outer = new Column { Children = { MakeTile(100f, 20f), inner } };
        outer.Spacing.Value = 5f;
        outer.Position.Value = new Vector2(200f, 300f);
        using var tree = Host(outer);

        // inner sits at (0, 25) in outer; tile at (50, 0) in inner.
        Assert.That(tile.GlobalRect, Is.EqualTo(new Rect(250f, 325f, 100f, 30f)));
    }
}
