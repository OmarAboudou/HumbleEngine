namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="Effect"/> and auto-tracking: an effect runs once,
/// then re-runs whenever a <see cref="Property{T}"/> it <b>read</b> changes —
/// without ever listing its dependencies. Dependencies are re-tracked each run
/// (so they can come and go), an unread property is ignored, and disposal stops it.
/// </summary>
public sealed class EffectTests
{
    [Test]
    public void Effect_RunsOnceImmediately()
    {
        var runs = 0;
        using var effect = new Effect(() => runs++);

        Assert.That(runs, Is.EqualTo(1));
    }

    [Test]
    public void Effect_RerunsWhenAReadPropertyChanges()
    {
        var name = new Property<string>("a");
        var seen = new List<string>();
        using var effect = new Effect(() => seen.Add(name.Value));

        name.Value = "b";
        name.Value = "c";

        Assert.That(seen, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Effect_RerunsOnAnyOfSeveralDependencies()
    {
        var a = new Property<int>(1);
        var b = new Property<int>(2);
        var sums = new List<int>();
        using var effect = new Effect(() => sums.Add(a.Value + b.Value));

        a.Value = 10; // 12
        b.Value = 20; // 30

        Assert.That(sums, Is.EqualTo(new[] { 3, 12, 30 }));
    }

    [Test]
    public void Effect_IgnoresAPropertyItNeverRead()
    {
        var read = new Property<int>(0);
        var unread = new Property<int>(0);
        var runs = 0;
        using var effect = new Effect(() => { _ = read.Value; runs++; });

        unread.Value = 5;

        Assert.That(runs, Is.EqualTo(1)); // only the initial run
    }

    [Test]
    public void Effect_RetracksDependencies_DroppingStaleOnes()
    {
        var useA = new Property<bool>(true);
        var a = new Property<int>(1);
        var b = new Property<int>(2);
        var runs = 0;
        using var effect = new Effect(() => { _ = useA.Value ? a.Value : b.Value; runs++; });
        // run 1: deps = {useA, a}

        useA.Value = false; // run 2: deps = {useA, b}
        a.Value = 100;      // a is no longer read → no re-run
        Assert.That(runs, Is.EqualTo(2));

        b.Value = 200;      // b is now read → re-run
        Assert.That(runs, Is.EqualTo(3));
    }

    [Test]
    public void Effect_Disposed_StopsRerunning()
    {
        var p = new Property<int>(0);
        var runs = 0;
        var effect = new Effect(() => { _ = p.Value; runs++; });

        p.Value = 1; // runs = 2
        effect.Dispose();
        p.Value = 2; // no re-run

        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void Effect_WritingAPropertyItReads_DoesNotLoop()
    {
        // The self-write invalidates the effect mid-run; the running guard drops
        // that re-entrant re-run rather than looping. (Don't write what you read.)
        var p = new Property<int>(0);
        var runs = 0;
        using var effect = new Effect(() => { runs++; if (p.Value < 3) p.Value = p.Value + 1; });

        Assert.That(runs, Is.EqualTo(1));
        Assert.That(p.Value, Is.EqualTo(1));
    }
}
