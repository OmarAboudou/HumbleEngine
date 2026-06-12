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
    [Fact]
    public void NodeList_Add_TransfersOwnership()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");

        panel.Children.Add(node);

        Assert.Same(panel, node.Parent);
        Assert.Equal(1, panel.Children.Count);
    }

    [Fact]
    public void NodeList_SupportsCollectionInitializer_InAddOrder()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");

        var panel = new TestPanel { Children = { a, b } };

        Assert.Same(panel, a.Parent);
        Assert.Same(panel, b.Parent);
        Assert.Same(a, panel.Children[0]);
        Assert.Same(b, panel.Children[1]);
    }

    [Fact]
    public void NodeList_Add_AdoptsFromAnotherParent()
    {
        var previous = new TestNode("previous");
        var node = new TestNode("node");
        previous.AttachChild(node);
        var panel = new TestPanel();

        panel.Children.Add(node);

        Assert.Same(panel, node.Parent);
        Assert.Empty(previous.ChildrenView);
    }

    [Fact]
    public void NodeList_Add_Duplicate_Throws()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        Assert.Throws<InvalidOperationException>(() => panel.Children.Add(node));
    }

    [Fact]
    public void NodeList_Remove_Detaches_NodeStaysAlive()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        Assert.True(panel.Children.Remove(node));

        Assert.Null(node.Parent);
        Assert.False(node.IsDisposed);
        Assert.Equal(0, panel.Children.Count);
        Assert.False(panel.Children.Remove(node));
    }

    [Fact]
    public void NodeList_Clear_DetachesAll()
    {
        var a = new TestNode("a");
        var b = new TestNode("b");
        var panel = new TestPanel { Children = { a, b } };

        panel.Children.Clear();

        Assert.Equal(0, panel.Children.Count);
        Assert.Null(a.Parent);
        Assert.Null(b.Parent);
    }

    [Fact]
    public void NodeList_PrunesMembersDisposedBehindItsBack()
    {
        var panel = new TestPanel();
        var node = new TestNode("a");
        panel.Children.Add(node);

        node.Dispose();

        Assert.Equal(0, panel.Children.Count);
        Assert.Empty(panel.Children);
    }

    [Fact]
    public void NodeSlot_Assign_TransfersOwnership()
    {
        var icon = new TestNode("sword");

        var card = new CardScene { Icon = icon };

        Assert.Same(card, icon.Parent);
        Assert.Same(icon, card.Icon);
    }

    [Fact]
    public void NodeSlot_Replace_DetachesPrevious_AliveAndParentless()
    {
        var first = new TestNode("first");
        var second = new TestNode("second");
        var card = new CardScene { Icon = first };

        card.Icon = second;

        Assert.Same(second, card.Icon);
        Assert.Null(first.Parent);
        Assert.False(first.IsDisposed);
    }

    [Fact]
    public void NodeSlot_AssignNull_EmptiesSlot()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        card.Icon = null;

        Assert.Null(card.Icon);
        Assert.Null(icon.Parent);
    }

    [Fact]
    public void NodeSlot_SelfHeals_WhenOccupantDisposedBehindItsBack()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        icon.Dispose();

        Assert.Null(card.Icon);
    }

    [Fact]
    public void Scene_OwnsSlotContent_DisposalCascades()
    {
        var icon = new TestNode("sword");
        var card = new CardScene { Icon = icon };

        card.Dispose();

        Assert.True(icon.IsDisposed); // the tree owns: injecting transferred ownership
    }

    [Fact]
    public void SceneSlot_CanMountDeepInsideThePrivateSubtree()
    {
        var item = new TestNode("item");

        var scene = new FramedScene { Items = { item } };

        // The member is a direct child of the *mount point*, not of the scene —
        // where the slot lands is the scene's private business.
        Assert.Equal("frame", item.Parent?.Name);
        Assert.NotSame(scene, item.Parent);
    }

    [Fact]
    public void SceneSlot_AcceptsRuntimeAdditions_SlotsAreLiveObjects()
    {
        var root = new TestNode("root");
        var scene = new FramedScene();
        root.AttachChild(scene);
        using var tree = new SceneTree { Root = root };

        // Long after construction: same API, same semantics, hooks fire.
        var late = new TestNode("late");
        scene.Items.Add(late);

        Assert.True(late.IsInTree);
        Assert.Equal("frame", late.Parent?.Name);
        Assert.Equal(1, scene.Items.Count);
    }

    [Fact]
    public void SlotContent_EntersLivingTree_WhenInjectedIntoLiveScene()
    {
        var root = new TestNode("root");
        var card = new CardScene();
        root.AttachChild(card);
        using var tree = new SceneTree { Root = root };

        var icon = new TestNode("sword");
        card.Icon = icon;

        Assert.True(icon.IsInTree);
        Assert.Same(tree, icon.Tree);
    }
}
