using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class ReactivePropertyTests
{
    [Test]
    public void Connect_NotifiesOnValueChange()
    {
        var prop = new ReactiveProperty<int>(0);
        int received = -1;
        prop.Connect(v => received = v);

        prop.Value = 42;

        Assert.That(received, Is.EqualTo(42));
    }

    [Test]
    public void Connect_DoesNotNotifyIfValueUnchanged()
    {
        var prop = new ReactiveProperty<int>(5);
        int callCount = 0;
        prop.Connect(_ => callCount++);

        prop.Value = 5;

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Disconnect_StopsNotifications()
    {
        var prop = new ReactiveProperty<string>("a");
        int callCount = 0;
        Action<string> listener = _ => callCount++;
        prop.Connect(listener);
        prop.Disconnect(listener);

        prop.Value = "b";

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void MultipleListeners_AllNotified()
    {
        var prop = new ReactiveProperty<int>(0);
        int a = 0, b = 0;
        prop.Connect(v => a = v);
        prop.Connect(v => b = v);

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

    [Test]
    public void ReactiveProperty_IsUsableAsISignal()
    {
        var prop = new ReactiveProperty<string>("a");
        ISignal<string> signal = prop;
        string? received = null;
        signal.Connect(v => received = v);

        prop.Value = "b";

        Assert.That(received, Is.EqualTo("b"));
    }
}
