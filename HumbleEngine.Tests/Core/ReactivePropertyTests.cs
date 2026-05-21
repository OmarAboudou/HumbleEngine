using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class ReactivePropertyTests
{
    [Test]
    public void Subscribe_NotifiesOnValueChange()
    {
        var prop = new ReactiveProperty<int>(0);
        int received = -1;
        prop.Subscribe(v => received = v);

        prop.Value = 42;

        Assert.That(received, Is.EqualTo(42));
    }

    [Test]
    public void Subscribe_DoesNotNotifyIfValueUnchanged()
    {
        var prop = new ReactiveProperty<int>(5);
        int callCount = 0;
        prop.Subscribe(_ => callCount++);

        prop.Value = 5;

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Unsubscribe_StopsNotifications()
    {
        var prop = new ReactiveProperty<string>("a");
        int callCount = 0;
        Action<string> listener = _ => callCount++;
        prop.Subscribe(listener);
        prop.Unsubscribe(listener);

        prop.Value = "b";

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void MultipleListeners_AllNotified()
    {
        var prop = new ReactiveProperty<int>(0);
        int a = 0, b = 0;
        prop.Subscribe(v => a = v);
        prop.Subscribe(v => b = v);

        prop.Value = 7;

        Assert.That(a, Is.EqualTo(7));
        Assert.That(b, Is.EqualTo(7));
    }

    [Test]
    public void BindFrom_SyncsImmediately()
    {
        var source = new ReactiveProperty<int>(10);
        var target = new ReactiveProperty<int>(0);

        target.BindFrom(source);

        Assert.That(target.Value, Is.EqualTo(10));
    }

    [Test]
    public void BindFrom_PropagatesFutureChanges()
    {
        var source = new ReactiveProperty<int>(0);
        var target = new ReactiveProperty<int>(0);
        target.BindFrom(source);

        source.Value = 99;

        Assert.That(target.Value, Is.EqualTo(99));
    }

    [Test]
    public void BindFrom_DoesNotPropagateBackToSource()
    {
        var source = new ReactiveProperty<int>(0);
        var target = new ReactiveProperty<int>(0);
        target.BindFrom(source);

        target.Value = 55;

        Assert.That(source.Value, Is.EqualTo(0));
    }
}
