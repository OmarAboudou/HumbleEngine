namespace HumbleEngine.Tests.SceneGraph;

/// <summary>Container exposing a typed children collection — the "open composition" case.</summary>
internal sealed class TestPanel : Node
{
    public new NodeList<TestNode> Children { get; }

    public TestPanel() => Children = CreateChildList<TestNode>();
}

/// <summary>Closed scene exposing one typed slot — the "scene contract" case.</summary>
internal sealed class CardScene : Scene
{
    private readonly NodeSlot<TestNode> _icon;

    public CardScene() => _icon = CreateChildSlot<TestNode>();

    public TestNode? Icon
    {
        get => _icon.Value;
        set => _icon.Value = value;
    }
}

/// <summary>
/// Closed scene mounting its slot <b>deep inside</b> its private subtree: the
/// public property delegates to an internal container's list. The injecting side
/// never knows where the content lands.
/// </summary>
internal sealed class FramedScene : Scene
{
    private readonly TestPanel _frame;

    public FramedScene()
    {
        _frame = new TestPanel { Name = "frame" };
        Attach(_frame);
    }

    /// <summary>Slot delegated to the internal mount point.</summary>
    public NodeList<TestNode> Items => _frame.Children;
}

/// <summary>
/// Unit tests for <see cref="NodeSlot{TChild}"/> and <see cref="NodeList{TChild}"/>:
/// ownership transfer, declarative initializers, self-healing, and the scene
/// slot contract.
/// </summary>
public sealed class SlotTests
{
    [Test]
    public void NodeList_Add_TransfersOwnership()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");

        panel.Children.Add(node);

        Assert.That(node.Parent, Is.SameAs(panel));
        Assert.That(panel.Children.Count, Is.EqualTo(1));
    }

    [Test]
    public void NodeList_SupportsCollectionInitializer_InAddOrder()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");

        var panel = new TestPanel { Children = { a, b } };

        Assert.That(a.Parent, Is.SameAs(panel));
        Assert.That(b.Parent, Is.SameAs(panel));
        Assert.That(panel.Children[0], Is.SameAs(a));
        Assert.That(panel.Children[1], Is.SameAs(b));
    }

    [Test]
    public void NodeList_Add_AdoptsFromAnotherParent()
    {
        var previous = new TestNode("previous");
        var node = new TestNode("node");
        previous.AttachChild(node);
        var panel = new TestPanel();

        panel.Children.Add(node);

        Assert.That(node.Parent, Is.SameAs(panel));
        Assert.That(previous.ChildrenView, Is.Empty);
    }

    [Test]
    public void NodeList_Add_Duplicate_Throws()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        Assert.Throws<InvalidOperationException>(() => panel.Children.Add(node));
    }

    [Test]
    public void NodeList_Remove_Detaches_NodeStaysAlive()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        Assert.That(panel.Children.Remove(node), Is.True);

        Assert.That(node.Parent, Is.Null);
        Assert.That(node.IsDisposed, Is.False);
        Assert.That(panel.Children.Count, Is.EqualTo(0));
        Assert.That(panel.Children.Remove(node), Is.False);
    }

    [Test]
    public void NodeList_Clear_DetachesAll()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var panel = new TestPanel { Children = { a, b } };

        panel.Children.Clear();

        Assert.That(panel.Children.Count, Is.EqualTo(0));
        Assert.That(a.Parent, Is.Null);
        Assert.That(b.Parent, Is.Null);
    }

    [Test]
    public void NodeList_PrunesMembersDisposedBehindItsBack()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        node.Dispose();

        Assert.That(panel.Children.Count, Is.EqualTo(0));
        Assert.That(panel.Children, Is.Empty);
    }

    [Test]
    public void NodeSlot_Assign_TransfersOwnership()
    {
        var icon = new TestNode("sword");

        var card = new CardScene { Icon = icon };

        Assert.That(icon.Parent, Is.SameAs(card));
        Assert.That(card.Icon, Is.SameAs(icon));
    }

    [Test]
    public void NodeSlot_Replace_DetachesPrevious_AliveAndParentless()
    {
        var first = new TestNode("first");
        var second = new TestNode("second");
        var card = new CardScene { Icon = first };

        card.Icon = second;

        Assert.That(card.Icon, Is.SameAs(second));
        Assert.That(first.Parent, Is.Null);
        Assert.That(first.IsDisposed, Is.False);
    }

    [Test]
    public void NodeSlot_AssignNull_EmptiesSlot()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        card.Icon = null;

        Assert.That(card.Icon, Is.Null);
        Assert.That(icon.Parent, Is.Null);
    }

    [Test]
    public void NodeSlot_SelfHeals_WhenOccupantDisposedBehindItsBack()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        icon.Dispose();

        Assert.That(card.Icon, Is.Null);
    }

    [Test]
    public void Scene_OwnsSlotContent_DisposalCascades()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        card.Dispose();

        Assert.That(icon.IsDisposed, Is.True); // the tree owns: injecting transferred ownership
    }

    [Test]
    public void SceneSlot_CanMountDeepInsideThePrivateSubtree()
    {
        var item = new TestNode("item");

        var scene = new FramedScene { Items = { item } };

        // The member is a direct child of the *mount point*, not of the scene —
        // where the slot lands is the scene's private business.
        Assert.That(item.Parent?.Name, Is.EqualTo("frame"));
        Assert.That(item.Parent, Is.Not.SameAs(scene));
    }

    [Test]
    public void SceneSlot_AcceptsRuntimeAdditions_SlotsAreLiveObjects()
    {
        var root = new TestNode("root");
        var scene = new FramedScene();
        root.AttachChild(scene);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        // Long after construction: same API, same semantics, hooks fire.
        var late = new TestNode("late");
        scene.Items.Add(late);

        Assert.That(late.IsInTree, Is.True);
        Assert.That(late.Parent?.Name, Is.EqualTo("frame"));
        Assert.That(scene.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public void SlotContent_EntersLivingTree_WhenInjectedIntoLiveScene()
    {
        var root = new TestNode("root");
        var card = new CardScene();
        root.AttachChild(card);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        var icon = new TestNode("sword");
        card.Icon = icon;

        Assert.That(icon.IsInTree, Is.True);
        Assert.That(icon.Tree, Is.SameAs(tree));
    }
}
