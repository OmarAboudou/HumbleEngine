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

    [Test]
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

        Assert.That(
            TreeHookEntries(log),
            Is.EqualTo(new[]
            {
                "root:Attaching", "child:Attaching", "grandchild:Attaching",
                "grandchild:Attached", "child:Attached", "root:Attached"
            }));
        Assert.That(grandchild.IsInTree, Is.True);
        Assert.That(grandchild.Tree, Is.SameAs(tree));
    }

    [Test]
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

        Assert.That(
            TreeHookEntries(log),
            Is.EqualTo(new[]
            {
                "child:Attaching", "grandchild:Attaching", "grandchild:Attached", "child:Attached"
            }));
    }

    [Test]
    public void ManipulatingDetachedSubtree_FiresNoTreeHooks()
    {
        var log = new List<string>();
        var parent = new TestNode("parent", log);
        var child = new TestNode("child", log);

        parent.AttachChild(child);
        parent.DetachChild(child);

        Assert.That(TreeHookEntries(log), Is.Empty);
        Assert.That(parent.IsInTree, Is.False);
    }

    [Test]
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

        Assert.That(
            TreeHookEntries(log),
            Is.EqualTo(new[]
            {
                "child:Detaching", "grandchild:Detaching", "grandchild:Detached", "child:Detached"
            }));
        Assert.That(child.Tree, Is.Null);
        Assert.That(grandchild.Tree, Is.Null);
    }

    [Test]
    public void OnDetaching_RunsWhileStillInTree_StructureIntact()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };

        root.DetachChild(child);

        Assert.That(child.WasInTreeDuringDetaching, Is.True);
    }

    [Test]
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

        Assert.That(TreeHookEntries(log), Is.Empty);
        Assert.That(log, Is.EqualTo(new[] { "child:ParentChanged" }));
        Assert.That(child.Tree, Is.SameAs(tree));
    }

    [Test]
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

        Assert.That(TreeHookEntries(log), Is.EqualTo(new[] { "child:Detaching", "child:Detached" }));
        Assert.That(child.Tree, Is.Null);
        Assert.That(child.Parent, Is.SameAs(detached));
    }

    [Test]
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

        Assert.That(TreeHookEntries(log), Is.EqualTo(new[] { "child:Attaching", "child:Attached" }));
        Assert.That(child.Tree, Is.SameAs(tree));
    }

    [Test]
    public void RootOfATree_CannotBeAttachedOrAdoptedElsewhere()
    {
        var root = new TestNode("root");
        using var tree = new SceneTree { Root = root };
        var other = new TestNode("other");

        Assert.Throws<InvalidOperationException>(() => other.AttachChild(root));
        Assert.Throws<InvalidOperationException>(() => other.AdoptChild(root));
    }

    [Test]
    public void Root_ParentedNode_Throws()
    {
        var parent = new TestNode("parent");
        var child = new TestNode("child");
        parent.AttachChild(child);

        using var tree = new SceneTree();
        Assert.Throws<InvalidOperationException>(() => tree.Root = child);
    }

    [Test]
    public void Root_OfAnotherTree_Throws()
    {
        var root = new TestNode("root");
        using var tree1 = new SceneTree { Root = root };
        using var tree2 = new SceneTree();

        Assert.Throws<InvalidOperationException>(() => tree2.Root = root);
    }

    [Test]
    public void ReplacingRoot_ExitsOldRoot_EntersNewRoot()
    {
        var log = new List<string>();
        var oldRoot = new TestNode("old", log);
        var newRoot = new TestNode("new", log);
        using var tree = new SceneTree { Root = oldRoot };
        log.Clear();

        tree.Root = newRoot;

        Assert.That(
            TreeHookEntries(log),
            Is.EqualTo(new[] { "old:Detaching", "old:Detached", "new:Attaching", "new:Attached" }));
        Assert.That(oldRoot.Tree, Is.Null);
        Assert.That(oldRoot.IsDisposed, Is.False); // stays alive, owned by its reference holder
        Assert.That(newRoot.Tree, Is.SameAs(tree));
    }

    [Test]
    public void QueueDispose_InTree_DefersUntilFlush()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };

        child.QueueDispose();
        Assert.That(child.IsDisposed, Is.False);

        tree.FlushDisposeQueue();
        Assert.That(child.IsDisposed, Is.True);
        Assert.That(child.Parent, Is.Null);
    }

    [Test]
    public void QueueDispose_DetachedNode_DisposesImmediately()
    {
        var node = new TestNode("node");
        node.QueueDispose();
        Assert.That(node.IsDisposed, Is.True);
    }

    [Test]
    public void DisposingInTreeNode_FiresExitHooks_BeforeDisposal()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var child = new TestNode("child", log);
        root.AttachChild(child);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        child.Dispose();

        Assert.That(
            log.Where(e => e.StartsWith("child:") && e != "child:ParentChanged"),
            Is.EqualTo(new[] { "child:Detaching", "child:Detached", "child:Disposed" }));
    }

    [Test]
    public void DisposingRootDirectly_ExitsTree_AndClearsRoot()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var tree = new SceneTree { Root = root };
        log.Clear();

        root.Dispose();

        Assert.That(TreeHookEntries(log), Is.EqualTo(new[] { "root:Detaching", "root:Detached" }));
        Assert.That(tree.Root, Is.Null);
        tree.Dispose();
    }

    [Test]
    public void TreeDispose_DisposesRootSubtree()
    {
        var root = new TestNode("root");
        var child = new TestNode("child");
        root.AttachChild(child);
        var tree = new SceneTree { Root = root };

        tree.Dispose();

        Assert.That(tree.IsDisposed, Is.True);
        Assert.That(root.IsDisposed, Is.True);
        Assert.That(child.IsDisposed, Is.True);
        Assert.That(tree.Root, Is.Null);
    }
}
