namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="NodeList{TChild}"/> as a reactive source: its
/// structure is auto-tracked just like an <see cref="ObservableList{T}"/>'s.
/// Reading it inside an <see cref="Effect"/> subscribes that effect, and a
/// membership change — adoption, removal, or a departure behind the list's back —
/// re-runs it. This is the machinery a <see cref="LinearContainer"/> relies on.
/// </summary>
public sealed class NodeListTrackingTests
{
    /// <summary>A node exposing a child list and the effect helper for the test.</summary>
    private sealed class ListNode : Node
    {
        public NodeList<Panel> Items { get; }

        public ListNode() => Items = CreateChildList<Panel>();

        public Effect WatchCount(Action<int> observe) => CreateEffect(() => observe(Items.Count));
    }

    [Test]
    public void Effect_RerunsWhenAChildJoinsOrLeaves()
    {
        var node = new ListNode();
        var counts = new List<int>();
        using var effect = node.WatchCount(counts.Add);

        var a = new Panel();
        node.Items.Add(a);          // 1
        node.Items.Add(new Panel()); // 2
        node.Items.Remove(a);        // 1

        Assert.That(counts, Is.EqualTo(new[] { 0, 1, 2, 1 }));
    }

    [Test]
    public void Effect_RerunsOnDepartureBehindTheListsBack()
    {
        var node = new ListNode();
        var a = new Panel();
        node.Items.Add(a);
        var counts = new List<int>();
        using var effect = node.WatchCount(counts.Add); // initial run: 1

        a.Dispose(); // not removed through the list; the broadcast narrates anyway

        Assert.That(counts, Is.EqualTo(new[] { 1, 0 }));
    }

    [Test]
    public void LayoutEffect_StopsWhenTheContainerIsDisposed()
    {
        var column = new Column();
        var child = new Panel();
        child.Size.Value = new Vector2(100f, 40f);
        column.Children.Add(child);
        Assert.That(child.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));

        column.Dispose();
        // The layout effect is gone with the node; mutating a former child's size
        // no longer triggers a relayout (nor throws).
        child.Size.Value = new Vector2(100f, 999f);

        Assert.That(child.Position.Value, Is.EqualTo(new Vector2(0f, 0f)));
    }
}
