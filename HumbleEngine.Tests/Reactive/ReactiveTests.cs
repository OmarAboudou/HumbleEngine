namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for the bare <see cref="Reactive{T}"/> cell: value storage,
/// push notification, equality gating and the re-entrancy guard.
/// </summary>
public sealed class ReactiveTests
{
    [Test]
    public void InitialValue_IsReadable()
    {
        var cell = new Reactive<int>(42);

        Assert.That(cell.Value, Is.EqualTo(42));
        Assert.That(cell.IsBound, Is.False);
    }

    [Test]
    public void Set_UpdatesValue_AndNotifiesWithNewValue()
    {
        var cell = new Reactive<string>("a");
        var received = new List<string>();
        cell.Changed += received.Add;

        cell.Value = "b";

        Assert.That(cell.Value, Is.EqualTo("b"));
        Assert.That(received, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void Set_SameValue_DoesNotNotify()
    {
        var cell = new Reactive<int>(5);
        var notifications = 0;
        cell.Changed += _ => notifications++;

        cell.Value = 5;

        Assert.That(notifications, Is.EqualTo(0));
    }

    [Test]
    public void Notification_IsSynchronous()
    {
        var cell = new Reactive<int>(0);
        var seenDuringSet = -1;
        cell.Changed += v => seenDuringSet = v;

        cell.Value = 7;

        Assert.That(seenDuringSet, Is.EqualTo(7));
    }

    [Test]
    public void ReentrantDivergingWrite_Throws()
    {
        var cell = new Reactive<int>(0);
        cell.Changed += v => cell.Value = v + 1;

        Assert.Throws<InvalidOperationException>(() => cell.Value = 1);
    }

    [Test]
    public void ReentrantConvergingWrite_StopsSilently()
    {
        var cell = new Reactive<int>(0);
        cell.Changed += v => cell.Value = v;

        cell.Value = 1;

        Assert.That(cell.Value, Is.EqualTo(1));
    }

    [Test]
    public void ManualCycle_BetweenTwoCells_ConvergesThroughEquality()
    {
        var a = new Reactive<int>(0);
        var b = new Reactive<int>(0);
        a.Changed += v => b.Value = v;
        b.Changed += v => a.Value = v;

        a.Value = 3;

        Assert.That(a.Value, Is.EqualTo(3));
        Assert.That(b.Value, Is.EqualTo(3));
    }
}
