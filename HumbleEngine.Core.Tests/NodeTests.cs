namespace HumbleEngine.Core.Tests;

[TestFixture]
public class NodeTests
{
    // ─── Hierarchy ────────────────────────────────────────────────────────────

    [Test]
    public void AddChild_ShouldAddChildToChildren()
    {
        var parent = new Node();
        var child  = new Node();

        parent.AddChild(child);

        Assert.That(parent.Children, Does.Contain(child));
    }

    [Test]
    public void AddChild_ShouldNotAddDuplicate()
    {
        var parent = new Node();
        var child  = new Node();

        parent.AddChild(child);
        parent.AddChild(child);

        Assert.That(parent.Children, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddChild_MultipleDistinctChildren_ShouldAllBePresent()
    {
        var parent = new Node();
        var childA = new Node();
        var childB = new Node();

        parent.AddChild(childA);
        parent.AddChild(childB);

        Assert.That(parent.Children, Has.Count.EqualTo(2));
        Assert.That(parent.Children, Does.Contain(childA));
        Assert.That(parent.Children, Does.Contain(childB));
    }

    [Test]
    public void RemoveChild_ExistingChild_ShouldRemoveFromChildren()
    {
        var parent = new Node();
        var child  = new Node();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(parent.Children, Does.Not.Contain(child));
    }

    [Test]
    public void RemoveChild_NonExistingChild_ShouldNotThrow()
    {
        var parent   = new Node();
        var stranger = new Node();

        Assert.That(() => parent.RemoveChild(stranger), Throws.Nothing);
    }

    [Test]
    public void RemoveChild_ShouldOnlyRemoveTargetChild()
    {
        var parent = new Node();
        var childA = new Node();
        var childB = new Node();
        parent.AddChild(childA);
        parent.AddChild(childB);

        parent.RemoveChild(childA);

        Assert.That(parent.Children, Does.Not.Contain(childA));
        Assert.That(parent.Children, Does.Contain(childB));
    }

    [Test]
    public void Children_ShouldBeEmptyByDefault()
    {
        var node = new Node();

        Assert.That(node.Children, Is.Empty);
    }

    [Test]
    public void Parent_ShouldBeNullByDefault()
    {
        var node = new Node();

        Assert.That(node.Parent, Is.Null);
    }

    // ─── LifeCycle (base) ─────────────────────────────────────────────────────

    [Test]
    public void TreeEntered_ShouldNotThrowOnBaseNode()
    {
        var node = new Node();

        Assert.That(() => node.TreeEntered(), Throws.Nothing);
    }

    [Test]
    public void Process_ShouldNotThrowOnBaseNode()
    {
        var node = new Node();

        Assert.That(() => node.Process(), Throws.Nothing);
    }

    [Test]
    public void TreeExiting_ShouldNotThrowOnBaseNode()
    {
        var node = new Node();

        Assert.That(() => node.TreeExiting(), Throws.Nothing);
    }

    // ─── LifeCycle (subclass override) ────────────────────────────────────────

    [Test]
    public void TreeEntered_ShouldBeOverridableInSubclass()
    {
        var node = new TrackingNode();

        node.TreeEntered();

        Assert.That(node.EnteredCalled, Is.True);
    }

    [Test]
    public void Process_ShouldBeOverridableInSubclass()
    {
        var node = new TrackingNode();

        node.Process();

        Assert.That(node.ProcessCalled, Is.True);
    }

    [Test]
    public void TreeExiting_ShouldBeOverridableInSubclass()
    {
        var node = new TrackingNode();

        node.TreeExiting();

        Assert.That(node.ExitingCalled, Is.True);
    }

    // ─── Helper subclass ──────────────────────────────────────────────────────

    private sealed class TrackingNode : Node
    {
        public bool EnteredCalled { get; private set; }
        public bool ProcessCalled { get; private set; }
        public bool ExitingCalled { get; private set; }

        public override void TreeEntered()  { EnteredCalled = true; }
        public override void Process()      { ProcessCalled = true; }
        public override void TreeExiting()  { ExitingCalled = true; }
    }
}