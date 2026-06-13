namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="global::HumbleEngine.Reactive.Untrack{T}(System.Func{T})"/>: reactive reads
/// performed inside the callback must not subscribe the surrounding computation.
/// </summary>
public sealed class UntrackTests
{
    [Test]
    public void Untrack_SuppressesSubscription_InsideEffect()
    {
        var tracked   = new Property<int>(0);
        var untracked = new Property<int>(0);
        var runs      = 0;

        using var effect = new Effect(() =>
        {
            runs++;
            _ = tracked.Value;                              // tracked dependency
            global::HumbleEngine.Reactive.Untrack(() => _ = untracked.Value);    // must NOT subscribe
        });

        Assert.That(runs, Is.EqualTo(1));

        untracked.Value = 1;        // no re-run: read happened untracked
        Assert.That(runs, Is.EqualTo(1));

        tracked.Value = 1;          // re-run: this one is a real dependency
        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void Untrack_ReturnsTheValue()
    {
        var prop = new Property<int>(7);

        var read = global::HumbleEngine.Reactive.Untrack(() => prop.Value);

        Assert.That(read, Is.EqualTo(7));
    }

    [Test]
    public void Untrack_RestoresContext_AfterCallback()
    {
        // A read after the Untrack block is tracked again.
        var inside  = new Property<int>(0);
        var outside = new Property<int>(0);
        var runs    = 0;

        using var effect = new Effect(() =>
        {
            runs++;
            global::HumbleEngine.Reactive.Untrack(() => _ = inside.Value);   // untracked
            _ = outside.Value;                          // tracked again afterwards
        });

        outside.Value = 1;
        Assert.That(runs, Is.EqualTo(2));   // outside is a dependency

        inside.Value = 1;
        Assert.That(runs, Is.EqualTo(2));   // inside is not
    }
}
