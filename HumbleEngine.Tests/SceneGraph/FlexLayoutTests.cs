namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the flex layout of <see cref="Row"/>/<see cref="Column"/>: under a
/// bounded main axis, non-flex children take their content size and the free space is
/// shared among the flex children (∝ factor); the cross axis is filled when bounded.
/// </summary>
public sealed class FlexLayoutTests
{
    // Hosts the root under a tight surface constraint (bounded both axes).
    private static SceneTree HostTight(Node root, float w, float h)
    {
        var tree = new SceneTree(new FakeRenderer());
        tree.SurfaceSize.Value = new Vector2(w, h);
        tree.Root = root;
        return tree;
    }

    private static Panel Fixed(float width)
    {
        var p = new Panel();
        p.Size.Value = new Vector2(width, 0f); // explicit width, height filled
        return p;
    }

    [Test]
    public void Expanded_TakesTheRemainingSpace()
    {
        var fixedChild = Fixed(100f);
        var grow       = new Panel();
        var row        = new Row();
        row.Add(fixedChild);
        row.Add(new Expanded(grow));
        using var tree = HostTight(row, 300f, 50f);

        Assert.That(fixedChild.Size.Value, Is.EqualTo(new Vector2(100f, 50f))); // width kept, height filled
        Assert.That(grow.Size.Value,       Is.EqualTo(new Vector2(200f, 50f))); // 300 - 100
        Assert.That(grow.Position.Value,   Is.EqualTo(new Vector2(100f, 0f)));
    }

    [Test]
    public void TwoExpanded_SplitTheSpaceEqually()
    {
        var a = new Panel();
        var b = new Panel();
        var row = new Row();
        row.Add(new Expanded(a));
        row.Add(new Expanded(b));
        using var tree = HostTight(row, 300f, 50f);

        Assert.That(a.Size.Value.X, Is.EqualTo(150f).Within(0.01f));
        Assert.That(b.Size.Value.X, Is.EqualTo(150f).Within(0.01f));
    }

    [Test]
    public void Factors_SplitProportionally()
    {
        var a = new Panel();
        var b = new Panel();
        var row = new Row();
        row.Add(new Expanded(a, factor: 1f));
        row.Add(new Expanded(b, factor: 2f));
        using var tree = HostTight(row, 300f, 50f);

        Assert.That(a.Size.Value.X, Is.EqualTo(100f).Within(0.01f)); // 1/3
        Assert.That(b.Size.Value.X, Is.EqualTo(200f).Within(0.01f)); // 2/3
    }

    [Test]
    public void Spacing_IsSubtractedFromTheFreeSpace()
    {
        var fixedChild = Fixed(100f);
        var grow       = new Panel();
        var row        = new Row { Spacing = { Value = 20f } };
        row.Add(fixedChild);
        row.Add(new Expanded(grow));
        using var tree = HostTight(row, 300f, 50f);

        // 300 - 100 (fixed) - 20 (spacing) = 180.
        Assert.That(grow.Size.Value.X,     Is.EqualTo(180f).Within(0.01f));
        Assert.That(grow.Position.Value.X, Is.EqualTo(120f).Within(0.01f)); // 100 + 20
    }

    [Test]
    public void CrossAxis_IsFilled()
    {
        var grow = new Panel();
        var row  = new Row();
        row.Add(new Expanded(grow));
        using var tree = HostTight(row, 300f, 80f);

        Assert.That(grow.Size.Value.Y, Is.EqualTo(80f)); // filled to the row's height
    }

    [Test]
    public void Flexible_StaysAtItsContentSize_WhenSmallerThanItsShare()
    {
        var small = new Panel();
        small.Size.Value = new Vector2(40f, 0f); // content width 40, well under the share
        var row = new Row();
        row.Add(new Flexible(small)); // loose fit
        using var tree = HostTight(row, 300f, 50f);

        // Loose flex: the child takes its content width, not the whole 300.
        Assert.That(small.Size.Value.X, Is.EqualTo(40f));
    }

    [Test]
    public void Column_DistributesVertically()
    {
        var top  = new Panel();
        top.Size.Value = new Vector2(0f, 60f); // explicit height
        var grow = new Panel();
        var column = new Column();
        column.Add(top);
        column.Add(new Expanded(grow));
        using var tree = HostTight(column, 100f, 200f);

        Assert.That(grow.Size.Value, Is.EqualTo(new Vector2(100f, 140f))); // 200-60 tall, width filled
        Assert.That(grow.Position.Value, Is.EqualTo(new Vector2(0f, 60f)));
    }
}
