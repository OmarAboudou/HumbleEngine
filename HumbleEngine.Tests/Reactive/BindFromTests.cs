namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for one-way bindings: immediate push at bind time, propagation
/// with transformation, the single-formula rule, fail-fast on manual writes,
/// bind-time cycle detection and deterministic release.
/// </summary>
public sealed class BindFromTests
{
    [Test]
    public void BindFrom_PushesCurrentValueImmediately()
    {
        var source = new Property<int>(21);
        var target = new Property<int>(0);

        target.BindFrom(source, v => v * 2);

        Assert.That(target.Value, Is.EqualTo(42));
        Assert.That(target.IsBound, Is.True);
        Assert.That(source.IsBound, Is.False);
    }

    [Test]
    public void BindFrom_PropagatesChanges_WithTransform()
    {
        var health = new Property<int>(100);
        var label = new Property<string>("");
        label.BindFrom(health, hp => $"PV : {hp}");

        health.Value = 80;

        Assert.That(label.Value, Is.EqualTo("PV : 80"));
    }

    [Test]
    public void BindFrom_Identity_Propagates()
    {
        var source = new Property<int>(1);
        var target = new Property<int>(0);
        target.BindFrom(source);

        source.Value = 9;

        Assert.That(target.Value, Is.EqualTo(9));
    }

    [Test]
    public void BoundTarget_ManualSet_Throws()
    {
        var source = new Property<int>(1);
        var target = new Property<int>(0);
        target.BindFrom(source);

        Assert.Throws<InvalidOperationException>(() => target.Value = 5);
        Assert.That(target.Value, Is.EqualTo(1));
    }

    [Test]
    public void Unbind_RestoresManualWrites_AndStopsPropagation()
    {
        var source = new Property<int>(1);
        var target = new Property<int>(0);
        target.BindFrom(source);

        target.Unbind();
        target.Value = 5;
        source.Value = 99;

        Assert.That(target.IsBound, Is.False);
        Assert.That(target.Value, Is.EqualTo(5));
    }

    [Test]
    public void Rebind_ReplacesPreviousBinding()
    {
        var first = new Property<int>(1);
        var second = new Property<int>(2);
        var target = new Property<int>(0);
        target.BindFrom(first);

        target.BindFrom(second);
        first.Value = 10;

        Assert.That(target.Value, Is.EqualTo(2));

        second.Value = 20;

        Assert.That(target.Value, Is.EqualTo(20));
    }

    [Test]
    public void BindFrom_Self_Throws()
    {
        var cell = new Property<int>(0);

        Assert.Throws<InvalidOperationException>(() => cell.BindFrom(cell));
    }

    [Test]
    public void BindFrom_ClosingACycle_ThrowsAtBindTime()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        var c = new Property<int>(0);
        b.BindFrom(c);
        a.BindFrom(b);

        Assert.Throws<InvalidOperationException>(() => c.BindFrom(a));
        Assert.That(c.IsBound, Is.False);
    }

    [Test]
    public void BindFrom_Mutual_ThrowsAtBindTime()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.BindFrom(b);

        Assert.Throws<InvalidOperationException>(() => b.BindFrom(a));
    }

    [Test]
    public void Source_RemainsListenableByThirdParties()
    {
        var origin = new Property<int>(1);
        var first = new Property<int>(0);
        var second = new Property<int>(0);
        first.BindFrom(origin);
        second.BindFrom(origin, v => -v);

        origin.Value = 4;

        Assert.That(first.Value, Is.EqualTo(4));
        Assert.That(second.Value, Is.EqualTo(-4));
    }

    [Test]
    public void Chain_PropagatesThrough()
    {
        var a = new Property<int>(1);
        var b = new Property<int>(0);
        var c = new Property<int>(0);
        b.BindFrom(a, v => v + 1);
        c.BindFrom(b, v => v * 10);

        a.Value = 4;

        Assert.That(b.Value, Is.EqualTo(5));
        Assert.That(c.Value, Is.EqualTo(50));
    }

    [Test]
    public void BindFrom_AcceptsReadOnlyView()
    {
        var source = new Property<int>(3);
        IReadOnlyProperty<int> view = source;
        var target = new Property<int>(0);

        target.BindFrom(view);
        source.Value = 8;

        Assert.That(target.Value, Is.EqualTo(8));
    }
}
