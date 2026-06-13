namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the node-level reactive helpers <see cref="Node"/> exposes:
/// an <see cref="Effect"/> or a <see cref="Computed{T}"/> created through them is
/// tied to the node's lifetime — disposing the node stops it reacting, so a dead
/// subtree neither reacts nor retains what it read.
/// </summary>
public sealed class NodeReactiveTests
{
    /// <summary>A node that exposes the protected reactive helpers for the test.</summary>
    private sealed class ReactiveNode : Node
    {
        public Effect MakeEffect(Action action) => CreateEffect(action);
        public Computed<T> MakeComputed<T>(Func<T> formula) => CreateComputed(formula);
    }

    [Test]
    public void CreateEffect_IsDisposedWithTheNode()
    {
        var node = new ReactiveNode();
        var p = new Property<int>(0);
        var runs = 0;
        node.MakeEffect(() => { _ = p.Value; runs++; });

        p.Value = 1; // runs = 2
        node.Dispose();
        p.Value = 2; // node gone → effect gone → no re-run

        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void CreateComputed_IsDisposedWithTheNode()
    {
        var node = new ReactiveNode();
        var p = new Property<int>(1);
        var doubled = node.MakeComputed(() => p.Value * 2);
        Assert.That(doubled.Value, Is.EqualTo(2));

        node.Dispose();
        p.Value = 10;

        Assert.That(doubled.Value, Is.EqualTo(2)); // frozen at the node's death
    }
}
