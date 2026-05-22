using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class NodeTests
{
    private class TestNode : Node
    {
        public bool InitCalled    { get; private set; }
        public bool DisposeCalled { get; private set; }
        public float LastDelta    { get; private set; }

        public override void Init()    => InitCalled = true;
        public override void Dispose() { DisposeCalled = true; base.Dispose(); }
        public override void Update(float delta) { LastDelta = delta; base.Update(delta); }
    }

    [Test]
    public void AddChild_SetsParent()
    {
        var parent = new TestNode();
        var child  = new TestNode();

        parent.AddChild(child);

        Assert.That(child.Parent, Is.SameAs(parent));
    }

    [Test]
    public void AddChild_CallsInitOnChild()
    {
        var parent = new TestNode();
        var child  = new TestNode();

        parent.AddChild(child);

        Assert.That(child.InitCalled, Is.True);
    }

    [Test]
    public void AddChild_AppearsInChildren()
    {
        var parent = new TestNode();
        var child  = new TestNode();

        parent.AddChild(child);

        Assert.That(parent.Children, Contains.Item(child));
    }

    [Test]
    public void AddChild_ThrowsIfNodeAlreadyHasParent()
    {
        var parentA = new TestNode();
        var parentB = new TestNode();
        var child   = new TestNode();
        parentA.AddChild(child);

        Assert.Throws<InvalidOperationException>(() => parentB.AddChild(child));
    }

    [Test]
    public void RemoveChild_ClearsParent()
    {
        var parent = new TestNode();
        var child  = new TestNode();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(child.Parent, Is.Null);
    }

    [Test]
    public void RemoveChild_CallsDisposeOnChild()
    {
        var parent = new TestNode();
        var child  = new TestNode();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(child.DisposeCalled, Is.True);
    }

    [Test]
    public void RemoveChild_RemovesFromChildren()
    {
        var parent = new TestNode();
        var child  = new TestNode();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(parent.Children, Does.Not.Contain(child));
    }

    [Test]
    public void MarkDirty_SetsIsDirty()
    {
        var node = new TestNode();

        node.MarkDirty();

        Assert.That(node.IsDirty, Is.True);
    }

    [Test]
    public void ClearDirty_ResetsIsDirty()
    {
        var node = new TestNode();
        node.MarkDirty();

        node.ClearDirty();

        Assert.That(node.IsDirty, Is.False);
    }

    [Test]
    public void Update_PropagatestoChildren()
    {
        var parent = new TestNode();
        var child  = new TestNode();
        parent.AddChild(child);

        parent.Update(0.16f);

        Assert.That(child.LastDelta, Is.EqualTo(0.16f).Within(0.0001f));
    }

    [Test]
    public void Dispose_PropagatestoChildren()
    {
        var parent = new TestNode();
        var child  = new TestNode();
        parent.AddChild(child);

        parent.Dispose();

        Assert.That(child.DisposeCalled, Is.True);
    }

    [Test]
    public void ReactiveProperty_MarksDirtyOnChange()
    {
        var node = new TestNode();
        var prop = new ReactiveProperty<string>("initial");
        prop.Connect(_ => node.MarkDirty());

        prop.Value = "changed";

        Assert.That(node.IsDirty, Is.True);
    }
}
