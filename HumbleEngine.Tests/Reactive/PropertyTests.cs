namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for the bare <see cref="Property{T}"/> cell: value storage,
/// push notification, equality gating and the re-entrancy guard.
/// </summary>
public sealed class PropertyTests
{
    [Test]
    public void InitialValue_IsReadable()
    {
        var cell = new Property<int>(42);

        Assert.That(cell.Value, Is.EqualTo(42));
        Assert.That(cell.IsBound, Is.False);
    }

    [Test]
    public void Set_UpdatesValue_AndNotifiesWithNewValue()
    {
        var cell = new Property<string>("a");
        var received = new List<string>();
        cell.Changed += received.Add;

        cell.Value = "b";

        Assert.That(cell.Value, Is.EqualTo("b"));
        Assert.That(received, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void Set_SameValue_DoesNotNotify()
    {
        var cell = new Property<int>(5);
        var notifications = 0;
        cell.Changed += _ => notifications++;

        cell.Value = 5;

        Assert.That(notifications, Is.EqualTo(0));
    }

    [Test]
    public void Notification_IsSynchronous()
    {
        var cell = new Property<int>(0);
        var seenDuringSet = -1;
        cell.Changed += v => seenDuringSet = v;

        cell.Value = 7;

        Assert.That(seenDuringSet, Is.EqualTo(7));
    }

    [Test]
    public void ReentrantDivergingWrite_Throws()
    {
        var cell = new Property<int>(0);
        cell.Changed += v => cell.Value = v + 1;

        Assert.Throws<InvalidOperationException>(() => cell.Value = 1);
    }

    [Test]
    public void ReentrantConvergingWrite_StopsSilently()
    {
        var cell = new Property<int>(0);
        cell.Changed += v => cell.Value = v;

        cell.Value = 1;

        Assert.That(cell.Value, Is.EqualTo(1));
    }

    [Test]
    public void ManualCycle_BetweenTwoCells_ConvergesThroughEquality()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.Changed += v => b.Value = v;
        b.Changed += v => a.Value = v;

        a.Value = 3;

        Assert.That(a.Value, Is.EqualTo(3));
        Assert.That(b.Value, Is.EqualTo(3));
    }

    // --- AsReadOnly (the enforced read-only view) ---

    [Test]
    public void AsReadOnly_ForwardsValueAndChanges()
    {
        var cell = new Property<int>(1);
        var view = cell.AsReadOnly();
        var seen = new List<int>();
        view.Changed += seen.Add;

        cell.Value = 2;

        Assert.That(view.Value, Is.EqualTo(2));
        Assert.That(seen, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void AsReadOnly_CannotBeCastBackToTheWritableCell()
    {
        var cell = new Property<int>(0);

        Assert.That(cell.AsReadOnly(), Is.Not.InstanceOf<Property<int>>());
    }

    [Test]
    public void AsReadOnly_ReturnsTheSameCachedView()
    {
        var cell = new Property<int>(0);

        Assert.That(cell.AsReadOnly(), Is.SameAs(cell.AsReadOnly()));
    }

    [Test]
    public void AsReadOnly_TracksThroughToTheSource()
    {
        var cell = new Property<int>(0);
        var view = cell.AsReadOnly();
        var runs = 0;
        using var effect = new Effect(() => { _ = view.Value; runs++; });

        cell.Value = 5; // an effect reading the view re-runs when the source changes

        Assert.That(runs, Is.EqualTo(2));
    }
}
