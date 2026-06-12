namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="SceneTree"/> and the tree-liveness lifecycle:
/// hook ordering, exact reparent semantics across the liveness boundary,
/// and deferred disposal.
/// </summary>
public sealed class SceneTreeTests
{
    private static IEnumerable<string> TreeHookEntries(IEnumerable<string> log) =>
        log.Where(e => e.EndsWith(":Attaching") || e.EndsWith(":Attached")
                    || e.EndsWith(":Detaching") || e.EndsWith(":Detached"));

    [Fact]
    public void SettingRoot_FiresEnterHooks_AttachingTopDown_AttachedBottomUp()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        var grandchild = new TestNode("grandchild", log);
        root.AttachChild(child);
        child.AttachChild(grandchild);
        log.Clear();

        using var tree = new SceneTree();
        tree.Root = root;

        Assert.Equal(
            ["root:Attaching", "child:Attaching", "grandchild:Attaching",
             "grandchild:Attached", "child:Attached", "root:Attached"],
            TreeHookEntries(log));
        Assert.True(grandchild.IsInTree);
        Assert.Same(tree, grandchild.Tree);
    }

    [Fact]
    public void AttachingSubtree_ToLiveParent_FiresHooksOnSubtreeOnly()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        var child = new TestNode("child", log);
        var grandchild = new TestNode("grandchild", log);
        child.AttachChild(grandchild);
        root.AttachChild(child);

        Assert.Equal(
            ["child:Attaching", "grandchild:Attaching", "grandchild:Attached", "child:Attached"],
            TreeHookEntries(log));
    }

    [Fact]
    public void ManipulatingDetachedSubtree_FiresNoTreeHooks()
    {
        var log = new List<string>();
        var parent = new TestNode("parent", log);
        var child = new TestNode("child", log);

        parent.AttachChild(child);
        parent.DetachChild(child);

        Assert.Empty(TreeHookEntries(log));
        Assert.False(parent.IsInTree);
    }

    [Fact]
    public void Detach_FromLiveTree_FiresExitHooks_DetachingTopDown_DetachedBottomUp()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        var grandchild = new TestNode("grandchild", log);
        root.AttachChild(child);
        child.AttachChild(grandchild);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        root.DetachChild(child);

        Assert.Equal(
            ["child:Detaching", "grandchild:Detaching", "grandchild:Detached", "child:Detached"],
            TreeHookEntries(log));
        Assert.Null(child.Tree);
        Assert.Null(grandchild.Tree);
    }

    [Fact]
    public void OnDetaching_RunsWhileStillInTree_StructureIntact()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };

        root.DetachChild(child);

        Assert.True(child.WasInTreeDuringDetaching);
    }

    [Fact]
    public void Adopt_WithinSameTree_FiresNoTreeHooks()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var a = new TestNode("a", log);
        var b = new TestNode("b", log);
        var child = new TestNode("child", log);
        root.AttachChild(a);
        root.AttachChild(b);
        a.AttachChild(child);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        b.AdoptChild(child);

        Assert.Empty(TreeHookEntries(log));
        Assert.Equal(["child:ParentChanged"], log);
        Assert.Same(tree, child.Tree);
    }

    [Fact]
    public void Adopt_FromLiveTreeToDetachedParent_FiresExitHooks()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };
        var detached = new TestNode("detached", log);
        log.Clear();

        detached.AdoptChild(child);

        Assert.Equal(["child:Detaching", "child:Detached"], TreeHookEntries(log));
        Assert.Null(child.Tree);
        Assert.Same(detached, child.Parent);
    }

    [Fact]
    public void Adopt_FromDetachedToLiveTree_FiresEnterHooks()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        using var tree = new SceneTree { Root = root };
        var detached = new TestNode("detached", log);
        var child = new TestNode("child", log);
        detached.AttachChild(child);
        log.Clear();

        root.AdoptChild(child);

        Assert.Equal(["child:Attaching", "child:Attached"], TreeHookEntries(log));
        Assert.Same(tree, child.Tree);
    }

    [Fact]
    public void RootOfATree_CannotBeAttachedOrAdoptedElsewhere()
    {
        var root = new TestNode("root");
        using var tree = new SceneTree { Root = root };
        var other = new TestNode("other");

        Assert.Throws<InvalidOperationException>(() => other.AttachChild(root));
        Assert.Throws<InvalidOperationException>(() => other.AdoptChild(root));
    }

    [Fact]
    public void Root_ParentedNode_Throws()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        using var tree = new SceneTree();
        Assert.Throws<InvalidOperationException>(() => tree.Root = child);
    }

    [Fact]
    public void Root_OfAnotherTree_Throws()
    {
        var root = new TestNode("root");
        using var tree1 = new SceneTree { Root = root };
        using var tree2 = new SceneTree();

        Assert.Throws<InvalidOperationException>(() => tree2.Root = root);
    }

    [Fact]
    public void ReplacingRoot_ExitsOldRoot_EntersNewRoot()
    {
        var log = new List<string>();
        var oldRoot = new TestNode("old", log);
        var newRoot = new TestNode("new", log);
        using var tree = new SceneTree { Root = oldRoot };
        log.Clear();

        tree.Root = newRoot;

        Assert.Equal(["old:Detaching", "old:Detached", "new:Attaching", "new:Attached"],
            TreeHookEntries(log));
        Assert.Null(oldRoot.Tree);
        Assert.False(oldRoot.IsDisposed); // stays alive, owned by its reference holder
        Assert.Same(tree, newRoot.Tree);
    }

    [Fact]
    public void QueueDispose_InTree_DefersUntilFlush()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };

        child.QueueDispose();
        Assert.False(child.IsDisposed);

        tree.FlushDisposeQueue();
        Assert.True(child.IsDisposed);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void QueueDispose_DetachedNode_DisposesImmediately()
    {
        var node = new TestNode("node");
        node.QueueDispose();
        Assert.True(node.IsDisposed);
    }

    [Fact]
    public void DisposingInTreeNode_FiresExitHooks_BeforeDisposal()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        child.Dispose();

        Assert.Equal(["child:Detaching", "child:Detached", "child:Disposed"],
            log.Where(e => e.StartsWith("child:") && e != "child:ParentChanged"));
    }

    [Fact]
    public void DisposingRootDirectly_ExitsTree_AndClearsRoot()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var tree = new SceneTree { Root = root };
        log.Clear();

        root.Dispose();

        Assert.Equal(["root:Detaching", "root:Detached"], TreeHookEntries(log));
        Assert.Null(tree.Root);
        tree.Dispose();
    }

    [Fact]
    public void TreeDispose_DisposesRootSubtree()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        var tree = new SceneTree { Root = root };

        tree.Dispose();

        Assert.True(tree.IsDisposed);
        Assert.True(root.IsDisposed);
        Assert.True(child.IsDisposed);
        Assert.Null(tree.Root);
    }
}
