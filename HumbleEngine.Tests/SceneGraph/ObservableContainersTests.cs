namespace HumbleEngine.Tests.SceneGraph;

/// <summary>Node owning a reactive cell — exercises the binding-lifetime contract.</summary>
internal sealed class StatNode : Node
{
    public Reactive<int> Health { get; }

    public StatNode() => Health = CreateReactive(100);
}

/// <summary>Container exposing its slot directly — the slot is itself a reactive cell.</summary>
internal sealed class IconHolder : Node
{
    public NodeSlot<TestNode> Icon { get; }

    public IconHolder() => Icon = CreateChildSlot<TestNode>();
}

/// <summary>
/// Unit tests for the convergence "observable + tree semantics": NodeList and
/// NodeSlot narrate every membership change at the moment it happens — explicit
/// operations, adoption elsewhere and disposal alike — and node death releases
/// cell bindings deterministically.
/// </summary>
public sealed class ObservableContainersTests
{
    private static List<string> Record(NodeList<TestNode> list)
    {
        var log = new List<string>();
        list.Added += (i, n) => log.Add($"add:{i}:{n.NodeName}");
        list.Removed += (i, n) => log.Add($"rem:{i}:{n.NodeName}");
        return log;
    }

    [Test]
    public void NodeList_Add_NarratesIndexAndItem()
    {
        var panel = new TestPanel();
        var log = Record(panel.Children);

        panel.Children.Add(new TestNode("a"));
        panel.Children.Add(new TestNode("b"));

        Assert.That(log, Is.EqualTo(new[] { "add:0:a", "add:1:b" }));
    }

    [Test]
    public void NodeList_Remove_NarratesDeparture()
    {
        var panel = new TestPanel();
        var a = new TestNode("a");
        var b = new TestNode("b");
        panel.Children.Add(a);
        panel.Children.Add(b);
        var log = Record(panel.Children);

        panel.Children.Remove(a);

        Assert.That(log, Is.EqualTo(new[] { "rem:0:a" }));
        Assert.That(panel.Children.Count, Is.EqualTo(1));
    }

    [Test]
    public void NodeList_AdoptionElsewhere_NarratesImmediately()
    {
        var panel = new TestPanel();
        var child = new TestNode("child");
        panel.Children.Add(child);
        var log = Record(panel.Children);
        var other = new TestNode("other");

        other.AdoptChild(child);

        Assert.That(log, Is.EqualTo(new[] { "rem:0:child" }));
        Assert.That(panel.Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void NodeList_MemberDisposed_NarratesImmediately()
    {
        var panel = new TestPanel();
        var child = new TestNode("child");
        panel.Children.Add(child);
        var log = Record(panel.Children);

        child.Dispose();

        Assert.That(log, Is.EqualTo(new[] { "rem:0:child" }));
        Assert.That(panel.Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void NodeList_Clear_NarratesFromEndToStart()
    {
        var panel = new TestPanel();
        panel.Children.Add(new TestNode("a"));
        panel.Children.Add(new TestNode("b"));
        var log = Record(panel.Children);

        panel.Children.Clear();

        Assert.That(log, Is.EqualTo(new[] { "rem:1:b", "rem:0:a" }));
    }

    [Test]
    public void NodeSlot_Assign_NarratesArrival()
    {
        var holder = new IconHolder();
        var narrated = new List<string?>();
        holder.Icon.Changed += n => narrated.Add(n?.NodeName);

        holder.Icon.Value = new TestNode("sword");

        Assert.That(narrated, Is.EqualTo(new[] { "sword" }));
    }

    [Test]
    public void NodeSlot_Replace_NarratesDepartureThenArrival()
    {
        var holder = new IconHolder();
        holder.Icon.Value = new TestNode("old");
        var narrated = new List<string?>();
        holder.Icon.Changed += n => narrated.Add(n?.NodeName);

        holder.Icon.Value = new TestNode("new");

        Assert.That(narrated, Is.EqualTo(new[] { null, "new" }));
    }

    [Test]
    public void NodeSlot_OccupantDisposed_NarratesNullImmediately()
    {
        var holder = new IconHolder();
        var icon = new TestNode("sword");
        holder.Icon.Value = icon;
        var narrations = 0;
        holder.Icon.Changed += n => { narrations++; Assert.That(n, Is.Null); };

        icon.Dispose();

        Assert.That(narrations, Is.EqualTo(1));
        Assert.That(holder.Icon.Value, Is.Null);
    }

    [Test]
    public void NodeSlot_IsABindingSource()
    {
        var holder = new IconHolder();
        var label = new Reactive<string>("");
        label.BindFrom(holder.Icon, icon => icon?.NodeName ?? "none");

        Assert.That(label.Value, Is.EqualTo("none"));

        holder.Icon.Value = new TestNode("sword");

        Assert.That(label.Value, Is.EqualTo("sword"));
    }

    [Test]
    public void DisposingNode_ReleasesItsCellBindings()
    {
        var external = new Reactive<int>(50);
        var node = new StatNode();
        node.Health.BindFrom(external);

        node.Dispose();
        external.Value = 75;

        Assert.That(node.Health.IsBound, Is.False);
        Assert.That(node.Health.Value, Is.EqualTo(50)); // frozen at death, no zombie writes
    }

    [Test]
    public void DisposingNode_ReleasesBothEndsOfItsTwoWayBinding()
    {
        var external = new Reactive<int>(50);
        var node = new StatNode();
        node.Health.BindTwoWayFrom(external);

        node.Dispose();

        Assert.That(external.IsBound, Is.False);
        external.Value = 75; // the survivor is free again
        Assert.That(node.Health.Value, Is.EqualTo(50));
    }

    [Test]
    public void DetachedNode_KeepsItsBindings()
    {
        var external = new Reactive<int>(50);
        var parent = new TestNode("parent");
        var node = new StatNode();
        parent.AttachChild(node);
        node.Health.BindFrom(external);

        parent.DetachChild(node);
        external.Value = 75;

        Assert.That(node.Health.Value, Is.EqualTo(75)); // alive and synchronizing
    }
}
