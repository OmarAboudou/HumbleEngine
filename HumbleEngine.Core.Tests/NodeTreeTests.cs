namespace HumbleEngine.Core.Tests;

file class NodeThatAddsChildOnTreeEntered(Node child) : Node
{
    public override void TreeEntered() => AddChild(child);
}

file class TrackingNode : Node
{
    public bool TreeEnteredCalled { get; private set; }
    public bool TreeExitingCalled { get; private set; }
    public double? LastProcessDelta { get; private set; }
    public double? LastPhysicsProcessDelta { get; private set; }

    public override void TreeEntered() => TreeEnteredCalled = true;
    public override void TreeExiting() => TreeExitingCalled = true;
    public override void Process(double delta) => LastProcessDelta = delta;
    public override void PhysicsProcess(double delta) => LastPhysicsProcessDelta = delta;
}

[TestFixture]
public class NodeTreeTests
{
    [Test]
    public void Constructor_RegistersAllNodesInSubtree()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);

        var tree = new NodeTree(root);

        Assert.That(root.Tree, Is.EqualTo(tree));
        Assert.That(child.Tree, Is.EqualTo(tree));
    }

    [Test]
    public void Constructor_RootWithParent_ThrowsArgumentException()
    {
        var parent = new Node();
        var child = new Node();
        parent.AddChild(child);

        Assert.Throws<ArgumentException>(() => new NodeTree(child));
    }

    [Test]
    public void Constructor_CallsTreeEnteredOnAllNodes()
    {
        var root = new TrackingNode();
        var child = new TrackingNode();
        root.AddChild(child);

        new NodeTree(root);

        Assert.That(root.TreeEnteredCalled, Is.True);
        Assert.That(child.TreeEnteredCalled, Is.True);
    }

    [Test]
    public void AddChild_AfterFlush_CallsTreeEnteredOnNewNode()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        var child = new TrackingNode();

        root.AddChild(child);
        tree.Process(0);

        Assert.That(child.TreeEnteredCalled, Is.True);
    }

    [Test]
    public void RemoveChild_AfterFlush_CallsTreeExitingOnRemovedNode()
    {
        var root = new Node();
        var child = new TrackingNode();
        root.AddChild(child);
        var tree = new NodeTree(root);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(child.TreeExitingCalled, Is.True);
    }

    [Test]
    public void RemoveChild_AfterFlush_ClearsTreeOnRemovedSubtree()
    {
        var root = new Node();
        var child = new Node();
        var grandChild = new Node();
        child.AddChild(grandChild);
        root.AddChild(child);
        var tree = new NodeTree(root);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(child.Tree, Is.Null);
        Assert.That(grandChild.Tree, Is.Null);
    }

    [Test]
    public void Process_CallsProcessOnAllNodes()
    {
        var root = new TrackingNode();
        var child = new TrackingNode();
        root.AddChild(child);
        var tree = new NodeTree(root);

        tree.Process(0.5);

        Assert.That(root.LastProcessDelta, Is.EqualTo(0.5));
        Assert.That(child.LastProcessDelta, Is.EqualTo(0.5));
    }

    [Test]
    public void PhysicsProcess_CallsPhysicsProcessOnAllNodes()
    {
        var root = new TrackingNode();
        var child = new TrackingNode();
        root.AddChild(child);
        var tree = new NodeTree(root);

        tree.PhysicsProcess(0.016);

        Assert.That(root.LastPhysicsProcessDelta, Is.EqualTo(0.016));
        Assert.That(child.LastPhysicsProcessDelta, Is.EqualTo(0.016));
    }

    [Test]
    public void GetNodesInPrefixOrder_ReturnsAllNodesFromRoot()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);
        var tree = new NodeTree(root);

        var result = tree.GetNodesInPrefixOrder();

        Assert.That(result, Is.EqualTo(new[] { root, child }));
    }

    [Test]
    public void Process_CommandsQueuedDuringFlush_AreNotExecutedInSameFlush()
    {
        var grandChild = new Node();
        var child = new NodeThatAddsChildOnTreeEntered(grandChild);
        var root = new Node();
        var tree = new NodeTree(root);
        root.AddChild(child);

        // Premier flush : ajoute child, qui dans TreeEntered queue l'ajout de grandChild
        tree.Process(0);
        Assert.That(child.Tree, Is.EqualTo(tree));
        Assert.That(grandChild.Tree, Is.Null); // pas encore exécuté

        // Deuxième flush : grandChild est maintenant ajouté
        tree.Process(0);
        Assert.That(grandChild.Tree, Is.EqualTo(tree));
    }
}