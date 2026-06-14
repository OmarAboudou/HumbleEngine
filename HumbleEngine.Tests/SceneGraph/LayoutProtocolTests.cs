namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the per-node layout protocol on <see cref="UINode"/>: once attached,
/// a node's <see cref="UINode.Size"/> is the result of <c>ComputeLayout(Incoming)</c>.
/// The default <c>ComputeLayout</c> honours the node's current size clamped to the
/// incoming constraint, so a not-yet-migrated node keeps its explicit size while it is
/// unconstrained, and is clamped once a parent constrains it.
/// </summary>
public sealed class LayoutProtocolTests
{
    [Test]
    public void Unbounded_PreservesAnExplicitSize()
    {
        var panel = new Panel();
        panel.Size.Value = new Vector2(30f, 20f);
        using var tree = new SceneTree(new FakeRenderer()) { Root = panel };

        // Incoming defaults to Unbounded → the layout is a no-op, the size stands.
        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(30f, 20f)));
    }

    [Test]
    public void TightIncoming_DictatesTheSize()
    {
        var panel = new Panel();
        using var tree = new SceneTree(new FakeRenderer()) { Root = panel };

        panel.Incoming.Value = Constraints.Tight(120f, 40f);

        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(120f, 40f)));
    }

    [Test]
    public void LooseIncoming_ClampsAnOversizedNode()
    {
        var panel = new Panel();
        panel.Size.Value = new Vector2(500f, 500f);
        using var tree = new SceneTree(new FakeRenderer()) { Root = panel };

        panel.Incoming.Value = Constraints.Loose(new Vector2(200f, 100f));

        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(200f, 100f)));
    }

    [Test]
    public void ChangingIncoming_RelaysOut()
    {
        var panel = new Panel();
        using var tree = new SceneTree(new FakeRenderer()) { Root = panel };

        panel.Incoming.Value = Constraints.Tight(100f, 50f);
        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(100f, 50f)));

        panel.Incoming.Value = Constraints.Tight(80f, 60f);
        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(80f, 60f)));
    }

    [Test]
    public void SettingSizeManually_DoesNotLoopThroughTheLayout()
    {
        // The default ComputeLayout reads Size untracked, so a manual Size write must
        // not re-trigger the layout effect (no feedback on a not-yet-migrated node).
        var panel = new Panel();
        using var tree = new SceneTree(new FakeRenderer()) { Root = panel };

        var changes = 0;
        panel.Size.Changed += _ => changes++;
        panel.Size.Value = new Vector2(42f, 7f);

        // Exactly one change: ours. No echo from the layout effect.
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(panel.Size.Value, Is.EqualTo(new Vector2(42f, 7f)));
    }
}
