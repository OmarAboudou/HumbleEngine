namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="IObservableValue"/> and <see cref="IObservableValue{T}"/>:
/// <see cref="Property{T}"/> and <see cref="Computed{T}"/> are inspectable through
/// the non-generic interface, and auto-tracking flows through the untyped
/// <see cref="IObservableValue.Value"/> getter.
/// </summary>
public sealed class ObservableValueTests
{
    [Test]
    public void Property_IsIObservableValueOfT()
    {
        var prop = new Property<float>(3.14f);

        Assert.That(prop, Is.InstanceOf<IObservableValue<float>>());
    }

    [Test]
    public void Property_IsIObservableValue()
    {
        var prop = new Property<string>("hello");

        Assert.That(prop, Is.InstanceOf<IObservableValue>());
    }

    [Test]
    public void Property_ValueType_ReturnsGenericArgument()
    {
        IObservableValue prop = new Property<float>(0f);

        Assert.That(prop.ValueType, Is.EqualTo(typeof(float)));
    }

    [Test]
    public void Property_UntypedValue_ReturnsCurrent()
    {
        IObservableValue prop = new Property<string>("abc");

        Assert.That(prop.Value, Is.EqualTo("abc"));
    }

    [Test]
    public void Property_UntypedValue_TracksInEffect()
    {
        var source = new Property<int>(1);
        IObservableValue observable = source;
        var seen = new List<object?>();

        using var effect = new Effect(() => seen.Add(observable.Value));

        source.Value = 2;
        source.Value = 3;

        Assert.That(seen, Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void Computed_IsIObservableValue()
    {
        var source = new Property<int>(5);
        using var computed = new Computed<string>(() => source.Value.ToString());

        IObservableValue observable = computed;

        Assert.That(observable.ValueType, Is.EqualTo(typeof(string)));
        Assert.That(observable.Value, Is.EqualTo("5"));
    }

    [Test]
    public void Computed_UntypedValue_TracksInEffect()
    {
        var source = new Property<int>(1);
        using var computed = new Computed<int>(() => source.Value * 2);
        IObservableValue observable = computed;
        var seen = new List<object?>();

        using var effect = new Effect(() => seen.Add(observable.Value));

        source.Value = 3;
        source.Value = 5;

        Assert.That(seen, Is.EqualTo(new object[] { 2, 6, 10 }));
    }

    [Test]
    public void TypedValue_Recovered_WithIsPattern()
    {
        IObservableValue prop = new Property<float>(1.5f);

        if (prop is IObservableValue<float> typed)
            Assert.That(typed.Value, Is.EqualTo(1.5f).Within(0.001f));
        else
            Assert.Fail("Expected IObservableValue<float>");
    }
}
