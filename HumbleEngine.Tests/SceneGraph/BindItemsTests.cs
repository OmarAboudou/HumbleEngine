namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <c>BindItemsFrom</c>: the positional mirror — the index is the
/// registry. Fabricate and adopt on insertion, dispose with the item on removal,
/// single-writer rule on the bound list, deterministic release with the owner.
/// </summary>
public sealed class BindItemsFromTests
{
    private static TestPanel BoundPanel(ObservableList<string> source, out List<TestNode> fabricated)
    {
        var panel = new TestPanel();
        var log = new List<TestNode>();
        panel.Children.BindItemsFrom(source, item =>
        {
            var node = new TestNode(item);
            log.Add(node);
            return node;
        });
        fabricated = log;
        return panel;
    }

    [Test]
    public void Bind_MirrorsExistingContent_Immediately()
    {
        var source = new ObservableList<string> { "a", "b" };

        var panel = BoundPanel(source, out var fabricated);

        Assert.That(panel.Children.Count, Is.EqualTo(2));
        Assert.That(panel.Children[0].NodeName, Is.EqualTo("a"));
        Assert.That(panel.Children[1].NodeName, Is.EqualTo("b"));
        Assert.That(fabricated, Has.Count.EqualTo(2));
        Assert.That(panel.Children[0].Parent, Is.SameAs(panel));
    }

    [Test]
    public void SourceAdd_FabricatesAndAdopts()
    {
        var source = new ObservableList<string>();
        var panel = BoundPanel(source, out _);

        source.Add("row");

        Assert.That(panel.Children.Count, Is.EqualTo(1));
        Assert.That(panel.Children[0].NodeName, Is.EqualTo("row"));
        Assert.That(panel.Children[0].Parent, Is.SameAs(panel));
    }

    [Test]
    public void SourceInsert_MirrorsThePosition()
    {
        var source = new ObservableList<string> { "a", "c" };
        var panel = BoundPanel(source, out _);

        source.Insert(1, "b");

        Assert.That(panel.Children.Select(n => n.NodeName), Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void SourceRemove_DisposesTheMirroredNode()
    {
        var source = new ObservableList<string> { "a", "b" };
        var panel = BoundPanel(source, out var fabricated);
        var doomed = fabricated[0];

        source.RemoveAt(0);

        Assert.That(doomed.IsDisposed, Is.True);
        Assert.That(panel.Children.Count, Is.EqualTo(1));
        Assert.That(panel.Children[0].NodeName, Is.EqualTo("b"));
    }

    [Test]
    public void SourceReplace_DisposesOldNode_FabricatesNew()
    {
        var source = new ObservableList<string> { "old" };
        var panel = BoundPanel(source, out var fabricated);
        var old = fabricated[0];

        source[0] = "new";

        Assert.That(old.IsDisposed, Is.True);
        Assert.That(panel.Children[0].NodeName, Is.EqualTo("new"));
        Assert.That(panel.Children.Count, Is.EqualTo(1));
    }

    [Test]
    public void BoundList_ManualMutations_Throw()
    {
        var source = new ObservableList<string> { "a" };
        var panel = BoundPanel(source, out _);

        Assert.Throws<InvalidOperationException>(() => panel.Children.Add(new TestNode("x")));
        Assert.Throws<InvalidOperationException>(() => panel.Children.Remove(panel.Children[0]));
        Assert.Throws<InvalidOperationException>(() => panel.Children.Clear());
    }

    [Test]
    public void Bind_OnNonEmptyList_Throws()
    {
        var panel = new TestPanel();
        panel.Children.Add(new TestNode("manual"));

        Assert.Throws<InvalidOperationException>(
            () => panel.Children.BindItemsFrom(new ObservableList<string>(), item => new TestNode(item)));
    }

    [Test]
    public void Bind_Twice_Throws()
    {
        var source = new ObservableList<string>();
        var panel = BoundPanel(source, out _);

        Assert.Throws<InvalidOperationException>(
            () => panel.Children.BindItemsFrom(source, item => new TestNode(item)));
    }

    [Test]
    public void Unbind_LeavesTheNodes_AndRestoresManualWrites()
    {
        var source = new ObservableList<string> { "a" };
        var panel = BoundPanel(source, out var fabricated);

        panel.Children.Unbind();
        source.Add("ignored");
        panel.Children.Add(new TestNode("manual"));

        Assert.That(panel.Children.IsBound, Is.False);
        Assert.That(fabricated[0].IsDisposed, Is.False); // the nodes are yours now
        Assert.That(panel.Children.Select(n => n.NodeName), Is.EqualTo(new[] { "a", "manual" }));
    }

    [Test]
    public void OwnerDispose_ReleasesTheMapping()
    {
        var source = new ObservableList<string> { "a" };
        var panel = BoundPanel(source, out var fabricated);

        panel.Dispose();
        source.Add("after-death");

        Assert.That(fabricated[0].IsDisposed, Is.True);   // died with its owner
        Assert.That(fabricated, Has.Count.EqualTo(1));    // the factory never ran again
    }

    [Test]
    public void DisposingAMirroredNode_Manually_Throws()
    {
        var source = new ObservableList<string> { "a" };
        _ = BoundPanel(source, out var fabricated);

        Assert.Throws<InvalidOperationException>(() => fabricated[0].Dispose());
    }

    [Test]
    public void ANodeList_CanBeTheSource_MirrorFollowsTreeChanges()
    {
        var panelA = new TestPanel();
        var panelB = new TestPanel();
        panelB.Children.BindItemsFrom(panelA.Children, a => new TestNode($"mirror-{a.NodeName}"));

        var original = new TestNode("x");
        panelA.Children.Add(original);

        Assert.That(panelB.Children[0].NodeName, Is.EqualTo("mirror-x"));
        var mirror = panelB.Children[0];

        // The original leaves panelA's subtree behind everyone's back: panelA's
        // list narrates the departure, the mapping hears it and disposes the mirror.
        new TestNode("elsewhere").AdoptChild(original);

        Assert.That(mirror.IsDisposed, Is.True);
        Assert.That(panelB.Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void BoundList_NarratesTheMirrorChanges()
    {
        var source = new ObservableList<string>();
        var panel = BoundPanel(source, out _);
        var log = new List<string>();
        panel.Children.Added += (i, n) => log.Add($"add:{i}:{n.NodeName}");
        panel.Children.Removed += (i, n) => log.Add($"rem:{i}:{n.NodeName}");

        source.Add("a");
        source.RemoveAt(0);

        Assert.That(log, Is.EqualTo(new[] { "add:0:a", "rem:0:a" }));
    }
}
