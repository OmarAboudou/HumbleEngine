namespace HumbleEngine.Core.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_InvokesConnectedDelegate()
    {
        Signal signal = this.CreateSignal("OnEvent");
        bool called = false;

        signal.Connect(() => called = true);
        this.Emit(signal);

        Assert.That(called, Is.True);
    }

    [Test]
    public void Emit_InvokesAllConnectedDelegates()
    {
        Signal signal = this.CreateSignal("OnEvent");
        int callCount = 0;

        signal.Connect(() => callCount++);
        signal.Connect(() => callCount++);
        this.Emit(signal);

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void Emit_DoesNotInvokeDisconnectedDelegate()
    {
        Signal signal = this.CreateSignal("OnEvent");
        bool called = false;
        var connection = signal.Connect(() => called = true);

        signal.Disconnect(connection);
        this.Emit(signal);

        Assert.That(called, Is.False);
    }

    [Test]
    public void Emit_ThrowsWhenCallerIsNotOwner()
    {
        Signal signal = this.CreateSignal("OnEvent");
        object notOwner = new();

        Assert.Throws<InvalidOperationException>(() => notOwner.Emit(signal));
    }

    [Test]
    public void Connect_ReturnsSameConnectionIfAlreadyConnected()
    {
        Signal signal = this.CreateSignal("OnEvent");
        Action handler = () => { };

        var first = signal.Connect(handler);
        var second = signal.Connect(handler);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Connect_DuplicateDoesNotAddConnection()
    {
        Signal signal = this.CreateSignal("OnEvent");
        int callCount = 0;
        Action handler = () => callCount++;

        signal.Connect(handler);
        signal.Connect(handler);
        this.Emit(signal);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Emit_PassesArgumentToDelegate()
    {
        Signal<int> signal = this.CreateSignal<int>("OnHit", "damage");
        int received = 0;

        signal.Connect(damage => received = damage);
        this.Emit(signal, 42);

        Assert.That(received, Is.EqualTo(42));
    }

    [Test]
    public void Emit_PassesBothArgumentsToDelegates()
    {
        Signal<int, string> signal = this.CreateSignal<int, string>("OnHit", "damage", "source");
        int receivedDamage = 0;
        string receivedSource = "";

        signal.Connect((damage, source) => { receivedDamage = damage; receivedSource = source; });
        this.Emit(signal, 42, "fire");

        Assert.That(receivedDamage, Is.EqualTo(42));
        Assert.That(receivedSource, Is.EqualTo("fire"));
    }

    [Test]
    public void Disconnect_ByDelegate_RemovesConnection()
    {
        Signal signal = this.CreateSignal("OnEvent");
        bool called = false;
        Action handler = () => called = true;

        signal.Connect(handler);
        signal.Disconnect(handler);
        this.Emit(signal);

        Assert.That(called, Is.False);
    }
}
