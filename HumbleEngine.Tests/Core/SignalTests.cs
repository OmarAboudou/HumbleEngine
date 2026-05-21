using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_NotifiesConnectedListeners()
    {
        var (signal, emit) = Signal.Create();
        int callCount = 0;
        signal.Connect(() => callCount++);

        emit();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Disconnect_StopsNotifications()
    {
        var (signal, emit) = Signal.Create();
        int callCount = 0;
        Action listener = () => callCount++;
        signal.Connect(listener);
        signal.Disconnect(listener);

        emit();

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Emit_NotifiesMultipleListeners()
    {
        var (signal, emit) = Signal.Create();
        int a = 0, b = 0;
        signal.Connect(() => a++);
        signal.Connect(() => b++);

        emit();

        Assert.That(a, Is.EqualTo(1));
        Assert.That(b, Is.EqualTo(1));
    }

    [Test]
    public void Generic_Emit_PassesValueToListeners()
    {
        var (signal, emit) = Signal<string>.Create();
        string? received = null;
        signal.Connect(v => received = v);

        emit("hello");

        Assert.That(received, Is.EqualTo("hello"));
    }

    [Test]
    public void Generic_Disconnect_StopsNotifications()
    {
        var (signal, emit) = Signal<int>.Create();
        int callCount = 0;
        Action<int> listener = _ => callCount++;
        signal.Connect(listener);
        signal.Disconnect(listener);

        emit(1);

        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Emit_IsNotAccessibleOnSignalType()
    {
        var (signal, _) = Signal.Create();
        Assert.That(signal.GetType().GetMethod("EmitCore",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
            Is.Null);
    }
}
