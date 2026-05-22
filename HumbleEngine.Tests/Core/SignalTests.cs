using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_NotifiesConnectedListener()
    {
        var mutable = new MutableSignal();
        int callCount = 0;
        mutable.Signal.Connect(() => callCount++);

        mutable.Emit();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Disconnect_StopsNotifications()
    {
        var mutable = new MutableSignal();
        int callCount = 0;
        Action listener = () => callCount++;
        mutable.Signal.Connect(listener);
        mutable.Signal.Disconnect(listener);

        mutable.Emit();

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Emit_NotifiesMultipleListeners()
    {
        var mutable = new MutableSignal();
        int a = 0, b = 0;
        mutable.Signal.Connect(() => a++);
        mutable.Signal.Connect(() => b++);

        mutable.Emit();

        Assert.That(a, Is.EqualTo(1));
        Assert.That(b, Is.EqualTo(1));
    }

    [Test]
    public void Generic_Emit_PassesValueToListener()
    {
        var mutable = new MutableSignal<string>();
        string? received = null;
        mutable.Signal.Connect(v => received = v);

        mutable.Emit("hello");

        Assert.That(received, Is.EqualTo("hello"));
    }

    [Test]
    public void Generic_Disconnect_StopsNotifications()
    {
        var mutable = new MutableSignal<int>();
        int callCount = 0;
        Action<int> listener = _ => callCount++;
        mutable.Signal.Connect(listener);
        mutable.Signal.Disconnect(listener);

        mutable.Emit(1);

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Signal_DoesNotExposeEmit()
    {
        var mutable = new MutableSignal();
        var signal  = mutable.Signal;

        var emitMethod = signal.GetType().GetMethod("Emit",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.That(emitMethod, Is.Null);
    }

    [Test]
    public void SignalProperty_ReturnsSameInstance()
    {
        var mutable = new MutableSignal();
        var first  = mutable.Signal;
        var second = mutable.Signal;

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void Signal_CannotBeCastToMutableSignal()
    {
        var mutable = new MutableSignal();
        ISignal signal = mutable.Signal;

        Assert.Throws<InvalidCastException>(() => _ = (MutableSignal)signal);
    }

    [Test]
    public void Signal_IsUsableAsISignal()
    {
        var mutable = new MutableSignal();
        ISignal signal = mutable.Signal;
        int callCount = 0;
        signal.Connect(() => callCount++);

        mutable.Emit();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void MutableSignal_IsUsableAsISignal()
    {
        var mutable = new MutableSignal();
        ISignal asInterface = mutable;
        int callCount = 0;
        asInterface.Connect(() => callCount++);

        mutable.Emit();

        Assert.That(callCount, Is.EqualTo(1));
    }
}
