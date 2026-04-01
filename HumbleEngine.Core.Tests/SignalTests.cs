namespace HumbleEngine.Core.Tests;

[TestFixture]
public class SignalTests
{
    [Test]
    public void Emit_InvokesConnectedDelegate()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        bool called = false;

        signal.Connect(() => called = true);
        signal.Emit();

        Assert.That(called, Is.True);
    }

    [Test]
    public void Emit_InvokesAllConnectedDelegates()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        int callCount = 0;

        signal.Connect(() => callCount++);
        signal.Connect(() => callCount++);
        signal.Emit();

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void Emit_DoesNotInvokeDisconnectedDelegate()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        bool called = false;
        var connection = signal.Connect(() => called = true);

        signal.Disconnect(connection);
        signal.Emit();

        Assert.That(called, Is.False);
    }

    [Test]
    public void Connect_ReturnsSameConnectionIfAlreadyConnected()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        Action handler = () => { };

        var first = signal.Connect(handler);
        var second = signal.Connect(handler);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void Connect_DuplicateDoesNotAddConnection()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        int callCount = 0;
        Action handler = () => callCount++;

        signal.Connect(handler);
        signal.Connect(handler);
        signal.Emit();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Emit_PassesArgumentToDelegate()
    {
        EmittableSignal<int> signal = this.CreateSignal<int>("OnHit", "damage");
        int received = 0;

        signal.Connect(damage => received = damage);
        signal.Emit(42);

        Assert.That(received, Is.EqualTo(42));
    }

    [Test]
    public void Emit_PassesBothArgumentsToDelegates()
    {
        EmittableSignal<int, string> signal = this.CreateSignal<int, string>("OnHit", "damage", "source");
        int receivedDamage = 0;
        string receivedSource = "";

        signal.Connect((damage, source) => { receivedDamage = damage; receivedSource = source; });
        signal.Emit(42, "fire");

        Assert.That(receivedDamage, Is.EqualTo(42));
        Assert.That(receivedSource, Is.EqualTo("fire"));
    }

    [Test]
    public void Disconnect_ByDelegate_RemovesConnection()
    {
        EmittableSignal signal = this.CreateSignal("OnEvent");
        bool called = false;
        Action handler = () => called = true;

        signal.Connect(handler);
        signal.Disconnect(handler);
        signal.Emit();

        Assert.That(called, Is.False);
    }
}
