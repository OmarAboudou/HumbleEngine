namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Node"/>: hierarchy invariants, parent-change
/// notifications, exact reparent semantics and recursive disposal.
/// </summary>
public sealed class NodeTests
{
    [Fact]
    public void Attach_SetsParent_AndAppendsChildInOrder()
    {
        var parent = new TestNode("parent");
        var first = new TestNode("first");
        var second = new TestNode("second");

        parent.AttachChild(first);
        parent.AttachChild(second);

        Assert.Same(parent, first.Parent);
        Assert.Equal([first, second], parent.ChildrenView);
    }

    [Fact]
    public void Attach_FiresOnParentChanged_FromNullToParent()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");

        parent.AttachChild(child);

        Assert.Equal((null, parent), child.LastParentChange);
        Assert.Equal(1, child.ParentChangeCount);
    }

    [Fact]
    public void Attach_NullChild_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TestNode("parent").AttachChild(null!));
    }

    [Fact]
    public void Attach_Self_Throws()
    {
        var node = new TestNode("node");
        Assert.Throws<InvalidOperationException>(() => node.AttachChild(node));
    }

    [Fact]
    public void Attach_AlreadyParentedNode_Throws()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var child = new TestNode("child");
        a.AttachChild(child);

        Assert.Throws<InvalidOperationException>(() => b.AttachChild(child));
    }

    [Fact]
    public void Attach_Ancestor_Throws_NoCycleAllowed()
    {
        var root = new TestNode("root");
        var middle = new TestNode("middle");
        var leaf = new TestNode("leaf");
        root.AttachChild(middle);
        middle.AttachChild(leaf);

        Assert.Throws<InvalidOperationException>(() => leaf.AttachChild(root));
    }

    [Fact]
    public void Attach_DisposedChild_Throws()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        child.Dispose();

        Assert.Throws<ObjectDisposedException>(() => parent.AttachChild(child));
    }

    [Fact]
    public void Attach_OnDisposedParent_Throws()
    {
        var parent = new TestNode("parent");
        parent.Dispose();

        Assert.Throws<ObjectDisposedException>(() => parent.AttachChild(new TestNode("child")));
    }

    [Fact]
    public void Detach_ClearsParent_AndFiresOnParentChanged()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        parent.DetachChild(child);

        Assert.Null(child.Parent);
        Assert.Empty(parent.ChildrenView);
        Assert.Equal((parent, null), child.LastParentChange);
    }

    [Fact]
    public void Detach_NonChild_Throws()
    {
        var parent = new TestNode("parent");
        Assert.Throws<InvalidOperationException>(() => parent.DetachChild(new TestNode("stranger")));
    }

    [Fact]
    public void Adopt_MovesNode_BetweenParents()
    {
        var oldParent = new TestNode("old");
        var newParent = new TestNode("new");
        var child = new TestNode("child");
        oldParent.AttachChild(child);

        newParent.AdoptChild(child);

        Assert.Same(newParent, child.Parent);
        Assert.Empty(oldParent.ChildrenView);
        Assert.Equal([child], newParent.ChildrenView);
    }

    [Fact]
    public void Adopt_FiresOnParentChanged_ExactlyOnce()
    {
        var oldParent = new TestNode("old");
        var newParent = new TestNode("new");
        var child = new TestNode("child");
        oldParent.AttachChild(child);

        newParent.AdoptChild(child);

        Assert.Equal((oldParent, newParent), child.LastParentChange);
        Assert.Equal(2, child.ParentChangeCount); // attach + reparent, nothing else
    }

    [Fact]
    public void Adopt_ToSameParent_IsNoOp()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        parent.AdoptChild(child);

        Assert.Equal(1, child.ParentChangeCount);
        Assert.Equal([child], parent.ChildrenView);
    }

    [Fact]
    public void Adopt_ParentlessNode_Attaches()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");

        parent.AdoptChild(child);

        Assert.Same(parent, child.Parent);
        Assert.Equal((null, parent), child.LastParentChange);
    }

    [Fact]
    public void Adopt_UnderOwnDescendant_Throws()
    {
        var root = new TestNode("root");
        var leaf = new TestNode("leaf");
        root.AttachChild(leaf);

        Assert.Throws<InvalidOperationException>(() => leaf.AdoptChild(root));
    }

    [Fact]
    public void Dispose_DisposesSubtree_ChildrenFirst()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        var grandchild = new TestNode("grandchild", log);
        root.AttachChild(child);
        child.AttachChild(grandchild);

        root.Dispose();

        Assert.True(root.IsDisposed);
        Assert.True(child.IsDisposed);
        Assert.True(grandchild.IsDisposed);
        Assert.Equal(["grandchild:Disposed", "child:Disposed", "root:Disposed"],
            log.Where(e => e.EndsWith(":Disposed")));
    }

    [Fact]
    public void Dispose_DetachesFromParent()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        child.Dispose();

        Assert.Null(child.Parent);
        Assert.Empty(parent.ChildrenView);
        Assert.False(parent.IsDisposed);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var log = new List<string>();
        var node = new TestNode("node", log);

        node.Dispose();
        node.Dispose();

        Assert.Equal(["node:Disposed"], log);
    }

    [Fact]
    public void ToString_UsesTypeAndOptionalName()
    {
        Assert.Equal("TestNode 'menu'", new TestNode("x") { Name = "menu" }.ToString());
        Assert.Equal("TestNode", new TestNode("x").ToString());
    }
}
