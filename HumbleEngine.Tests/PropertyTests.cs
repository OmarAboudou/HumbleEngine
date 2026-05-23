namespace HumbleEngine.Tests;

[TestFixture]
public class PropertyTests
{
    [Test]
    public void Value_ReturnsInitialValue()
    {
        var prop = new Property<int>(42);
        Assert.That(prop.Value, Is.EqualTo(42));
    }

    [Test]
    public void Value_Set_UpdatesValue()
    {
        var prop = new Property<int>();
        prop.Value = 10;
        Assert.That(prop.Value, Is.EqualTo(10));
    }

    [Test]
    public void ValueChanged_FiresWithOldAndNewValue()
    {
        var prop = new Property<int>(1);
        int oldVal = 0, newVal = 0;
        prop.ValueChanged.Connect((o, n) => { oldVal = o; newVal = n; });
        prop.Value = 2;
        Assert.That(oldVal, Is.EqualTo(1));
        Assert.That(newVal, Is.EqualTo(2));
    }

    [Test]
    public void ValueChanged_DoesNotFireWhenSameValueAssigned()
    {
        var prop = new Property<int>(5);
        int calls = 0;
        prop.ValueChanged.Connect((_, _) => calls++);
        prop.Value = 5;
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void AsReadOnly_ReflectsCurrentValue()
    {
        var prop = new Property<int>(3);
        var ro = prop.AsReadOnly();
        prop.Value = 7;
        Assert.That(ro.Value, Is.EqualTo(7));
    }

    [Test]
    public void AsReadOnly_ValueChanged_FiresWhenUnderlyingChanges()
    {
        var prop = new Property<string>("a");
        var ro = prop.AsReadOnly();
        string? received = null;
        ro.ValueChanged.Connect((_, n) => received = n);
        prop.Value = "b";
        Assert.That(received, Is.EqualTo("b"));
    }

    [Test]
    public void AsReadOnly_ReturnsSameInstance()
    {
        var prop = new Property<int>();
        Assert.That(prop.AsReadOnly(), Is.SameAs(prop.AsReadOnly()));
    }
}
