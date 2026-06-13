namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="Computed{T}"/>: a derived value that auto-tracks its
/// formula's reads, recomputes when they change, notifies only on a real change,
/// and is itself a source — so Property → Computed → Effect chains propagate.
/// </summary>
public sealed class ComputedTests
{
    [Test]
    public void Computed_HasItsInitialValueFromTheFormula()
    {
        var a = new Property<int>(2);
        var b = new Property<int>(3);
        using var sum = new Computed<int>(() => a.Value + b.Value);

        Assert.That(sum.Value, Is.EqualTo(5));
    }

    [Test]
    public void Computed_RecomputesWhenADependencyChanges()
    {
        var a = new Property<int>(2);
        var b = new Property<int>(3);
        using var sum = new Computed<int>(() => a.Value + b.Value);

        a.Value = 10;

        Assert.That(sum.Value, Is.EqualTo(13));
    }

    [Test]
    public void Computed_FiresChanged_WithTheNewValue()
    {
        var first = new Property<string>("a");
        var last = new Property<string>("b");
        using var full = new Computed<string>(() => $"{first.Value} {last.Value}");
        var seen = new List<string>();
        full.Changed += v => seen.Add(v);

        first.Value = "x";

        Assert.That(full.Value, Is.EqualTo("x b"));
        Assert.That(seen, Is.EqualTo(new[] { "x b" }));
    }

    [Test]
    public void Property_Through_Computed_To_Effect_Propagates()
    {
        var n = new Property<int>(1);
        using var doubled = new Computed<int>(() => n.Value * 2);
        var seen = new List<int>();
        using var effect = new Effect(() => seen.Add(doubled.Value));

        n.Value = 5; // doubled → 10 → the effect re-runs

        Assert.That(seen, Is.EqualTo(new[] { 2, 10 }));
    }

    [Test]
    public void Computed_DoesNotNotify_WhenTheDerivedValueIsUnchanged()
    {
        var x = new Property<int>(4);
        using var parity = new Computed<int>(() => x.Value % 2); // 0
        var runs = 0;
        using var effect = new Effect(() => { _ = parity.Value; runs++; });

        x.Value = 6; // still even → parity unchanged → no re-run
        Assert.That(runs, Is.EqualTo(1));

        x.Value = 7; // now odd → parity changes → re-run
        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void Computed_ChainsThroughComputed()
    {
        var a = new Property<int>(1);
        using var b = new Computed<int>(() => a.Value + 1);  // 2
        using var c = new Computed<int>(() => b.Value * 10); // 20

        Assert.That(c.Value, Is.EqualTo(20));

        a.Value = 5; // b → 6, c → 60
        Assert.That(c.Value, Is.EqualTo(60));
    }

    [Test]
    public void Computed_Disposed_StopsRecomputing()
    {
        var p = new Property<int>(1);
        var doubled = new Computed<int>(() => p.Value * 2);
        Assert.That(doubled.Value, Is.EqualTo(2));

        doubled.Dispose();
        p.Value = 10;

        Assert.That(doubled.Value, Is.EqualTo(2)); // frozen at disposal
    }
}
