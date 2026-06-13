namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Node.Children"/> — the public, read-only, observable
/// view of a node's real subtree (doctrine "closed to writing, open to reading").
/// It reflects every membership change (attach, detach, adopt, disposal), narrates
/// insertions/removals with their index, auto-tracks its structure, and is a valid
/// <c>BindItemsFrom</c> source.
/// </summary>
public sealed class NodeChildrenTests
{
    [Test]
    public void Children_StartEmpty()
    {
        var node = new TestNode("n");

        Assert.That(node.Children, Is.Empty);
        Assert.That(node.Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void Children_ReflectAttachInOrder()
    {
        var parent = new TestNode("p");
        var a = new TestNode("a");
        var b = new TestNode("b");

        parent.AttachChild(a);
        parent.AttachChild(b);

        Assert.That(parent.Children, Is.EqualTo(new Node[] { a, b }));
    }

    [Test]
    public void Children_ReflectDetach()
    {
        var parent = new TestNode("p");
        var a = new TestNode("a");
        var b = new TestNode("b");
        parent.AttachChild(a);
        parent.AttachChild(b);

        parent.DetachChild(a);

        Assert.That(parent.Children, Is.EqualTo(new Node[] { b }));
    }

    [Test]
    public void Children_NarrateInsertionAndRemoval_WithIndex()
    {
        var parent = new TestNode("p");
        var log = new List<string>();
        parent.Children.Added += (i, _) => log.Add($"add:{i}");
        parent.Children.Removed += (i, _) => log.Add($"rem:{i}");
        var a = new TestNode("a");
        var b = new TestNode("b");

        parent.AttachChild(a); // add:0
        parent.AttachChild(b); // add:1
        parent.DetachChild(a); // rem:0

        Assert.That(log, Is.EqualTo(new[] { "add:0", "add:1", "rem:0" }));
    }

    [Test]
    public void Children_ReflectAdopt_OnBothParents()
    {
        var p1 = new TestNode("p1");
        var p2 = new TestNode("p2");
        var c = new TestNode("c");
        p1.AttachChild(c);

        p2.AdoptChild(c);

        Assert.That(p1.Children, Is.Empty);
        Assert.That(p2.Children, Is.EqualTo(new Node[] { c }));
    }

    [Test]
    public void Children_ReflectChildDisposal()
    {
        var parent = new TestNode("p");
        var a = new TestNode("a");
        parent.AttachChild(a);

        a.Dispose(); // detaches from the parent on its way out

        Assert.That(parent.Children, Is.Empty);
    }

    [Test]
    public void Children_AreObservable_EffectRerunsOnMembershipChange()
    {
        var parent = new TestNode("p");
        var counts = new List<int>();
        using var effect = new Effect(() => counts.Add(parent.Children.Count));

        parent.AttachChild(new TestNode("a")); // 1
        var b = new TestNode("b");
        parent.AttachChild(b);                 // 2
        parent.DetachChild(b);                 // 1

        Assert.That(counts, Is.EqualTo(new[] { 0, 1, 2, 1 }));
    }

    [Test]
    public void Children_AreAValidBindItemsFromSource()
    {
        // The editor's hierarchy panel client: mirror one node's real children into
        // a container, kept in sync by the exact narration.
        var source = new TestNode("src");
        source.AttachChild(new TestNode("a"));
        var mirror = new Column();
        mirror.Children.BindItemsFrom(source.Children, _ => new Panel());
        Assert.That(mirror.Children.Count, Is.EqualTo(1));

        source.AttachChild(new TestNode("b"));
        Assert.That(mirror.Children.Count, Is.EqualTo(2));

        source.DetachChild(source.Children[0]);
        Assert.That(mirror.Children.Count, Is.EqualTo(1));
    }

    [Test]
    public void Children_AreTheSameViewInstance_EachAccess()
    {
        var node = new TestNode("n");

        Assert.That(node.Children, Is.SameAs(node.Children));
    }
}
