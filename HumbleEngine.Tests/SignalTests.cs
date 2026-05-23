namespace HumbleEngine.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_CallsConnectedCallback()
    {
        var signal = new Signal();
        int calls = 0;
        signal.Connect(() => calls++);
        signal.Emit();
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Emit_CallsAllConnectedCallbacks()
    {
        var signal = new Signal();
        int calls = 0;
        signal.Connect(() => calls++);
        signal.Connect(() => calls++);
        signal.Emit();
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void Disconnect_StopsCallbackFromFiring()
    {
        var signal = new Signal();
        int calls = 0;
        Action cb = () => calls++;
        signal.Connect(cb);
        signal.Disconnect(cb);
        signal.Emit();
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void Emit_DisconnectDuringEmit_DoesNotThrow()
    {
        var signal = new Signal();
        Action? cb = null;
        cb = () => signal.Disconnect(cb!);
        signal.Connect(cb);
        Assert.DoesNotThrow(() => signal.Emit());
    }

    [Test]
    public void Emit_ConnectDuringEmit_NewCallbackNotCalledInCurrentEmit()
    {
        var signal = new Signal();
        int calls = 0;
        signal.Connect(() => signal.Connect(() => calls++));
        signal.Emit();
        Assert.That(calls, Is.EqualTo(0));
        signal.Emit();
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void SignalT_EmitsCorrectValue()
    {
        var signal = new Signal<int>();
        int received = 0;
        signal.Connect(v => received = v);
        signal.Emit(42);
        Assert.That(received, Is.EqualTo(42));
    }

    [Test]
    public void SignalT1T2_EmitsCorrectValues()
    {
        var signal = new Signal<int, string>();
        int receivedInt = 0;
        string? receivedStr = null;
        signal.Connect((i, s) => { receivedInt = i; receivedStr = s; });
        signal.Emit(7, "hello");
        Assert.That(receivedInt, Is.EqualTo(7));
        Assert.That(receivedStr, Is.EqualTo("hello"));
    }
}
