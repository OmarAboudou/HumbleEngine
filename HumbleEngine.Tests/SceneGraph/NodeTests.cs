namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Node"/>: hierarchy invariants, parent-change
/// notifications, exact reparent semantics and recursive disposal.
/// </summary>
public sealed class NodeTests
{
    [Test]
    public void Attach_SetsParent_AndAppendsChildInOrder()
    {
        var parent = new TestNode("parent");
        var first = new TestNode("first");
        var second = new TestNode("second");

        parent.AttachChild(first);
        parent.AttachChild(second);

        Assert.That(first.Parent, Is.SameAs(parent));
        Assert.That(parent.ChildrenView, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Attach_FiresOnParentChanged_FromNullToParent()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");

        parent.AttachChild(child);

        Assert.That(child.LastParentChange, Is.EqualTo(((Node?)null, (Node?)parent)));
        Assert.That(child.ParentChangeCount, Is.EqualTo(1));
    }

    [Test]
    public void Attach_NullChild_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestNode("parent").AttachChild(null!));
    }

    [Test]
    public void Attach_Self_Throws()
    {
        var node = new TestNode("node");
        Assert.Throws<InvalidOperationException>(() => node.AttachChild(node));
    }

    [Test]
    public void Attach_AlreadyParentedNode_Throws()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var child = new TestNode("child");
        a.AttachChild(child);

        Assert.Throws<InvalidOperationException>(() => b.AttachChild(child));
    }

    [Test]
    public void Attach_Ancestor_Throws_NoCycleAllowed()
    {
        var root = new TestNode("root");
        var middle = new TestNode("middle");
        var leaf = new TestNode("leaf");
        root.AttachChild(middle);
        middle.AttachChild(leaf);

        Assert.Throws<InvalidOperationException>(() => leaf.AttachChild(root));
    }

    [Test]
    public void Attach_DisposedChild_Throws()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        child.Dispose();

        Assert.Throws<ObjectDisposedException>(() => parent.AttachChild(child));
    }

    [Test]
    public void Attach_OnDisposedParent_Throws()
    {
        var parent = new TestNode("parent");
        parent.Dispose();

        Assert.Throws<ObjectDisposedException>(() => parent.AttachChild(new TestNode("child")));
    }

    [Test]
    public void Detach_ClearsParent_AndFiresOnParentChanged()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        parent.DetachChild(child);

        Assert.That(child.Parent, Is.Null);
        Assert.That(parent.ChildrenView, Is.Empty);
        Assert.That(child.LastParentChange, Is.EqualTo(((Node?)parent, (Node?)null)));
    }

    [Test]
    public void Detach_NonChild_Throws()
    {
        var parent = new TestNode("parent");
        Assert.Throws<InvalidOperationException>(() => parent.DetachChild(new TestNode("stranger")));
    }

    [Test]
    public void Adopt_MovesNode_BetweenParents()
    {
        var oldParent = new TestNode("old");
        var newParent = new TestNode("new");
        var child = new TestNode("child");
        oldParent.AttachChild(child);

        newParent.AdoptChild(child);

        Assert.That(child.Parent, Is.SameAs(newParent));
        Assert.That(oldParent.ChildrenView, Is.Empty);
        Assert.That(newParent.ChildrenView, Is.EqualTo(new[] { child }));
    }

    [Test]
    public void Adopt_FiresOnParentChanged_ExactlyOnce()
    {
        var oldParent = new TestNode("old");
        var newParent = new TestNode("new");
        var child = new TestNode("child");
        oldParent.AttachChild(child);

        newParent.AdoptChild(child);

        Assert.That(child.LastParentChange, Is.EqualTo(((Node?)oldParent, (Node?)newParent)));
        Assert.That(child.ParentChangeCount, Is.EqualTo(2)); // attach + reparent, nothing else
    }

    [Test]
    public void Adopt_ToSameParent_IsNoOp()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        parent.AdoptChild(child);

        Assert.That(child.ParentChangeCount, Is.EqualTo(1));
        Assert.That(parent.ChildrenView, Is.EqualTo(new[] { child }));
    }

    [Test]
    public void Adopt_ParentlessNode_Attaches()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");

        parent.AdoptChild(child);

        Assert.That(child.Parent, Is.SameAs(parent));
        Assert.That(child.LastParentChange, Is.EqualTo(((Node?)null, (Node?)parent)));
    }

    [Test]
    public void Adopt_UnderOwnDescendant_Throws()
    {
        var root = new TestNode("root");
        var leaf = new TestNode("leaf");
        root.AttachChild(leaf);

        Assert.Throws<InvalidOperationException>(() => leaf.AdoptChild(root));
    }

    [Test]
    public void Dispose_DisposesSubtree_ChildrenFirst()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        var grandchild = new TestNode("grandchild", log);
        root.AttachChild(child);
        child.AttachChild(grandchild);

        root.Dispose();

        Assert.That(root.IsDisposed, Is.True);
        Assert.That(child.IsDisposed, Is.True);
        Assert.That(grandchild.IsDisposed, Is.True);
        Assert.That(log.Where(e => e.EndsWith(":Disposed")),
            Is.EqualTo(new[] { "grandchild:Disposed", "child:Disposed", "root:Disposed" }));
    }

    [Test]
    public void Dispose_DetachesFromParent()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        child.Dispose();

        Assert.That(child.Parent, Is.Null);
        Assert.That(parent.ChildrenView, Is.Empty);
        Assert.That(parent.IsDisposed, Is.False);
    }

    [Test]
    public void Dispose_IsIdempotent()
    {
        var log = new List<string>();
        var node = new TestNode("node", log);

        node.Dispose();
        node.Dispose();

        Assert.That(log, Is.EqualTo(new[] { "node:Disposed" }));
    }

    [Test]
    public void ToString_UsesTypeAndOptionalName()
    {
        Assert.That(new TestNode("x") { Name = "menu" }.ToString(), Is.EqualTo("TestNode 'menu'"));
        Assert.That(new TestNode("x").ToString(), Is.EqualTo("TestNode"));
    }
}
