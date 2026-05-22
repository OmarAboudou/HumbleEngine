using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_NotifiesConnectedListener()
    {
        var emitter = new SignalEmitter();
        int callCount = 0;
        emitter.Signal.Connect(() => callCount++);

        emitter.Emit();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Disconnect_StopsNotifications()
    {
        var emitter = new SignalEmitter();
        int callCount = 0;
        Action listener = () => callCount++;
        emitter.Signal.Connect(listener);
        emitter.Signal.Disconnect(listener);

        emitter.Emit();

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Emit_NotifiesMultipleListeners()
    {
        var emitter = new SignalEmitter();
        int a = 0, b = 0;
        emitter.Signal.Connect(() => a++);
        emitter.Signal.Connect(() => b++);

        emitter.Emit();

        Assert.That(a, Is.EqualTo(1));
        Assert.That(b, Is.EqualTo(1));
    }

    [Test]
    public void Generic_Emit_PassesValueToListener()
    {
        var emitter = new SignalEmitter<string>();
        string? received = null;
        emitter.Signal.Connect(v => received = v);

        emitter.Emit("hello");

        Assert.That(received, Is.EqualTo("hello"));
    }

    [Test]
    public void Generic_Disconnect_StopsNotifications()
    {
        var emitter = new SignalEmitter<int>();
        int callCount = 0;
        Action<int> listener = _ => callCount++;
        emitter.Signal.Connect(listener);
        emitter.Signal.Disconnect(listener);

        emitter.Emit(1);

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Signal_DoesNotExposeEmit()
    {
        var emitter = new SignalEmitter();
        var signal  = emitter.Signal;

        var emitMethod = signal.GetType().GetMethod("Emit",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.That(emitMethod, Is.Null);
    }

    [Test]
    public void SignalProperty_ReturnsSameInstance()
    {
        var emitter = new SignalEmitter();
        var first   = emitter.Signal;
        var second  = emitter.Signal;

        Assert.That(first, Is.SameAs(second));
    }
}
