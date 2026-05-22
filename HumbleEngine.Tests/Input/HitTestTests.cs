namespace HumbleEngine.Tests;

[TestFixture]
public class HitTestTests
{
    private class TestNode : Node
    {
        public TestNode(Rect bounds, HitTestFilter filter = HitTestFilter.Stop)
        {
            ComputedBounds = bounds;
            Filter = filter;
        }
        private readonly HitTestFilter Filter;
        public override HitTestFilter MouseFilter => Filter;
        public int ClickCount { get; private set; }
        public override void OnClick() => ClickCount++;
    }

    // --- HitTest.Find ---

    [Test]
    public void Find_PointInsideRoot_ReturnsRoot()
    {
        var root = new TestNode(new Rect(0, 0, 200, 200));
        Assert.That(HitTest.Find(root, 100, 100), Is.SameAs(root));
    }

    [Test]
    public void Find_PointOutsideAll_ReturnsNull()
    {
        var root = new TestNode(new Rect(0, 0, 100, 100));
        Assert.That(HitTest.Find(root, 200, 200), Is.Null);
    }

    [Test]
    public void Find_ReturnsDeepestNode()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200));
        var child = new TestNode(new Rect(50, 50, 100, 100));
        root.AddChild(child);

        Assert.That(HitTest.Find(root, 75, 75), Is.SameAs(child));
    }

    [Test]
    public void Find_ZOrder_LastChildWins()
    {
        var root   = new TestNode(new Rect(0, 0, 200, 200));
        var first  = new TestNode(new Rect(0, 0, 100, 100));
        var second = new TestNode(new Rect(0, 0, 100, 100));
        root.AddChild(first);
        root.AddChild(second);

        Assert.That(HitTest.Find(root, 50, 50), Is.SameAs(second));
    }

    [Test]
    public void Find_IgnoresHitTestFilter()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200));
        var child = new TestNode(new Rect(50, 50, 100, 100), HitTestFilter.Ignore);
        root.AddChild(child);

        // Find retourne le node même si Ignore
        Assert.That(HitTest.Find(root, 75, 75), Is.SameAs(child));
    }

    // --- HitTest.GetHoveredPath ---

    [Test]
    public void GetHoveredPath_ReturnsAncestorChain()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200));
        var child = new TestNode(new Rect(0, 0, 100, 100));
        root.AddChild(child);

        var path = HitTest.GetHoveredPath(child);

        Assert.That(path, Has.Count.EqualTo(2));
        Assert.That(path[0], Is.SameAs(child));
        Assert.That(path[1], Is.SameAs(root));
    }

    [Test]
    public void GetHoveredPath_ExcludesDisabled()
    {
        var root     = new TestNode(new Rect(0, 0, 200, 200), HitTestFilter.Disabled);
        var child    = new TestNode(new Rect(0, 0, 100, 100));
        root.AddChild(child);

        var path = HitTest.GetHoveredPath(child);

        Assert.That(path, Has.Count.EqualTo(1));
        Assert.That(path[0], Is.SameAs(child));
    }

    [Test]
    public void GetHoveredPath_NullNode_ReturnsEmpty()
    {
        var path = HitTest.GetHoveredPath(null);
        Assert.That(path, Is.Empty);
    }

    // --- HitTest.FirstInteractive ---

    [Test]
    public void FirstInteractive_ReturnsFirstStopOrPass()
    {
        var stop   = new TestNode(new Rect(0, 0, 100, 100), HitTestFilter.Stop);
        var ignore = new TestNode(new Rect(0, 0, 50, 50), HitTestFilter.Ignore);
        stop.AddChild(ignore);

        var path = HitTest.GetHoveredPath(ignore);
        Assert.That(HitTest.FirstInteractive(path), Is.SameAs(stop));
    }

    [Test]
    public void FirstInteractive_AllIgnore_ReturnsNull()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200), HitTestFilter.Ignore);
        var child = new TestNode(new Rect(0, 0, 100, 100), HitTestFilter.Ignore);
        root.AddChild(child);

        var path = HitTest.GetHoveredPath(child);
        Assert.That(HitTest.FirstInteractive(path), Is.Null);
    }

    // --- HitTest.DispatchClick ---

    [Test]
    public void DispatchClick_Stop_FiresOnce()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200), HitTestFilter.Stop);
        var child = new TestNode(new Rect(0, 0, 100, 100), HitTestFilter.Stop);
        root.AddChild(child);

        HitTest.DispatchClick(child);

        Assert.That(child.ClickCount, Is.EqualTo(1));
        Assert.That(root.ClickCount,  Is.EqualTo(0));
    }

    [Test]
    public void DispatchClick_Pass_PropagatesUp()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200), HitTestFilter.Stop);
        var child = new TestNode(new Rect(0, 0, 100, 100), HitTestFilter.Pass);
        root.AddChild(child);

        HitTest.DispatchClick(child);

        Assert.That(child.ClickCount, Is.EqualTo(1));
        Assert.That(root.ClickCount,  Is.EqualTo(1));
    }

    [Test]
    public void DispatchClick_Ignore_Skips()
    {
        var root  = new TestNode(new Rect(0, 0, 200, 200), HitTestFilter.Stop);
        var child = new TestNode(new Rect(0, 0, 100, 100), HitTestFilter.Ignore);
        root.AddChild(child);

        HitTest.DispatchClick(child);

        Assert.That(child.ClickCount, Is.EqualTo(0));
        Assert.That(root.ClickCount,  Is.EqualTo(1));
    }
}
