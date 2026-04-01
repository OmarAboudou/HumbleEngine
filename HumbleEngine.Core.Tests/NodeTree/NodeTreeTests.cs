namespace HumbleEngine.Core.Tests;

file class NodeThatAddsChildOnTreeEntered(Node child) : Node
{
    public override void TreeEntered() => AddChild(child);
}

file class OrderTrackingNode(List<Node> callOrder, string trackedCallback) : Node
{
    public override void TreeEntered() { if (trackedCallback == nameof(TreeEntered)) callOrder.Add(this); }
    public override void Ready() { if (trackedCallback == nameof(Ready)) callOrder.Add(this); }
    public override void Unready() { if (trackedCallback == nameof(Unready)) callOrder.Add(this); }
    public override void TreeExiting() { if (trackedCallback == nameof(TreeExiting)) callOrder.Add(this); }
}

file class TrackingNode : Node
{
    public bool TreeEnteredCalled { get; private set; }
    public bool TreeExitingCalled { get; private set; }
    public bool ReadyCalled { get; private set; }
    public bool UnreadyCalled { get; private set; }
    public double? LastProcessDelta { get; private set; }
    public double? LastPhysicsProcessDelta { get; private set; }

    public override void TreeEntered() => TreeEnteredCalled = true;
    public override void TreeExiting() => TreeExitingCalled = true;
    public override void Ready() => ReadyCalled = true;
    public override void Unready() => UnreadyCalled = true;
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
    public void Constructor_CallsReadyOnAllNodes()
    {
        var root = new TrackingNode();
        var child = new TrackingNode();
        root.AddChild(child);

        new NodeTree(root);

        Assert.That(root.ReadyCalled, Is.True);
        Assert.That(child.ReadyCalled, Is.True);
    }

    [Test]
    public void RemoveChild_AfterFlush_CallsUnreadyOnRemovedNodes()
    {
        var root = new Node();
        var child = new TrackingNode();
        root.AddChild(child);
        var tree = new NodeTree(root);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(child.UnreadyCalled, Is.True);
    }

    [Test]
    public void RemoveChild_AfterFlush_CallsUnreadyInPrefixOrder()
    {
        var callOrder = new List<Node>();
        var root = new OrderTrackingNode(callOrder, nameof(Node.Unready));
        var a = new OrderTrackingNode(callOrder, nameof(Node.Unready));
        var c = new OrderTrackingNode(callOrder, nameof(Node.Unready));
        a.AddChild(c);
        root.AddChild(a);
        var tree = new NodeTree(root);

        root.RemoveChild(a);
        tree.Process(0);

        // Préfixe de a : a, c → Unready dans cet ordre
        Assert.That(callOrder, Is.EqualTo(new[] { a, c }));
    }

    [Test]
    public void AddChild_AfterFlush_CallsReadyOnNewNode()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        var child = new TrackingNode();

        root.AddChild(child);
        tree.Process(0);

        Assert.That(child.ReadyCalled, Is.True);
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
    public void Constructor_CallsReadyInReversePrefixOrder()
    {
        var callOrder = new List<Node>();
        var root = new OrderTrackingNode(callOrder, nameof(Node.Ready));
        var a = new OrderTrackingNode(callOrder, nameof(Node.Ready));
        var c = new OrderTrackingNode(callOrder, nameof(Node.Ready));
        a.AddChild(c);
        root.AddChild(a);

        new NodeTree(root);

        // Préfixe de root : root, a, c → inverse : c, a, root
        Assert.That(callOrder, Is.EqualTo(new[] { c, a, root }));
    }

    [Test]
    public void RemoveChild_AfterFlush_CallsTreeExitingInReversePrefixOrder()
    {
        var callOrder = new List<Node>();
        var root = new OrderTrackingNode(callOrder, nameof(Node.TreeExiting));
        var a = new OrderTrackingNode(callOrder, nameof(Node.TreeExiting));
        var b = new OrderTrackingNode(callOrder, nameof(Node.TreeExiting));
        var c = new OrderTrackingNode(callOrder, nameof(Node.TreeExiting));
        a.AddChild(c);
        root.AddChild(a);
        root.AddChild(b);
        var tree = new NodeTree(root);

        root.RemoveChild(a);
        tree.Process(0);

        // Préfixe de a : a, c → inverse : c, a
        Assert.That(callOrder, Is.EqualTo(new[] { c, a }));
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
    public void OnTreeEntered_EmittedWhenNodeEntersTree()
    {
        var root = new Node();
        bool called = false;
        root.OnTreeEntered.Connect(() => called = true);

        new NodeTree(root);

        Assert.That(called, Is.True);
    }

    [Test]
    public void OnReady_EmittedWhenNodeIsReady()
    {
        var root = new Node();
        bool called = false;
        root.OnReady.Connect(() => called = true);

        new NodeTree(root);

        Assert.That(called, Is.True);
    }

    [Test]
    public void OnUnready_EmittedWhenNodeIsUnreadied()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);
        var tree = new NodeTree(root);
        bool called = false;
        child.OnUnready.Connect(() => called = true);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(called, Is.True);
    }

    [Test]
    public void OnTreeExiting_EmittedWhenNodeExitsTree()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);
        var tree = new NodeTree(root);
        bool called = false;
        child.OnTreeExiting.Connect(() => called = true);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(called, Is.True);
    }

    [Test]
    public void OnNodeAdded_EmittedWhenChildAdded_AfterFlush()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        var child = new Node();
        Node? received = null;
        tree.OnNodeAdded.Connect(node => received = node);

        root.AddChild(child);
        tree.Process(0);

        Assert.That(received, Is.EqualTo(child));
    }

    [Test]
    public void OnNodeRemoved_EmittedWhenChildRemoved_AfterFlush()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);
        var tree = new NodeTree(root);
        Node? received = null;
        tree.OnNodeRemoved.Connect(node => received = node);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(received, Is.EqualTo(child));
    }

    [Test]
    public void OnNodeRenamed_EmittedWithNodeAndNewName_WhenNodeInTreeIsRenamed()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        Node? receivedNode = null;
        string? receivedName = null;
        tree.OnNodeRenamed.Connect((node, name) => { receivedNode = node; receivedName = name; });

        root.Name = "Player";

        Assert.That(receivedNode, Is.EqualTo(root));
        Assert.That(receivedName, Is.EqualTo("Player"));
    }

    [Test]
    public void OnNodeRenamed_NotEmitted_AfterNodeLeavesTree()
    {
        var root = new Node();
        var child = new Node();
        root.AddChild(child);
        var tree = new NodeTree(root);
        bool called = false;
        tree.OnNodeRenamed.Connect((_, _) => called = true);

        root.RemoveChild(child);
        tree.Process(0);
        child.Name = "Orphan";

        Assert.That(called, Is.False);
    }

    [Test]
    public void OnTreeChanged_EmittedOnce_WhenSubtreeAdded()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        var child = new Node();
        var grandChild = new Node();
        child.AddChild(grandChild);
        int callCount = 0;
        tree.OnTreeChanged.Connect(() => callCount++);

        root.AddChild(child);
        tree.Process(0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void OnTreeChanged_EmittedOnce_WhenSubtreeRemoved()
    {
        var root = new Node();
        var child = new Node();
        var grandChild = new Node();
        child.AddChild(grandChild);
        root.AddChild(child);
        var tree = new NodeTree(root);
        int callCount = 0;
        tree.OnTreeChanged.Connect(() => callCount++);

        root.RemoveChild(child);
        tree.Process(0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void OnTreeChanged_EmittedWhenNodeRenamed()
    {
        var root = new Node();
        var tree = new NodeTree(root);
        bool called = false;
        tree.OnTreeChanged.Connect(() => called = true);

        root.Name = "Player";

        Assert.That(called, Is.True);
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