namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for two-way bindings: initial source of truth, echo suppression
/// (one trip, never a bounce — even with non-inverse transformations), the
/// both-slots-occupied rule, and release of both ends at once.
/// </summary>
public sealed class BindTwoWayTests
{
    [Test]
    public void BindTwoWayFrom_InitialSync_TakesArgumentValue()
    {
        var ui = new Property<string>("stale");
        var model = new Property<string>("truth");

        ui.BindTwoWayFrom(model);

        Assert.That(ui.Value, Is.EqualTo("truth"));
        Assert.That(ui.IsBound, Is.True);
        Assert.That(model.IsBound, Is.True);
    }

    [Test]
    public void TwoWay_PropagatesBothDirections()
    {
        var ui = new Property<string>("");
        var model = new Property<string>("a");
        ui.BindTwoWayFrom(model);

        model.Value = "from model";
        Assert.That(ui.Value, Is.EqualTo("from model"));

        ui.Value = "from ui";
        Assert.That(model.Value, Is.EqualTo("from ui"));
    }

    [Test]
    public void TwoWay_ManualWrites_AllowedOnBothEnds()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(1);
        a.BindTwoWayFrom(b);

        a.Value = 5;
        b.Value = 9;

        Assert.That(a.Value, Is.EqualTo(9));
        Assert.That(b.Value, Is.EqualTo(9));
    }

    [Test]
    public void TwoWay_AppliesTransformations()
    {
        var celsius = new Property<double>(0);
        var fahrenheit = new Property<double>(212);
        celsius.BindTwoWayFrom(fahrenheit, f => (f - 32) * 5 / 9, c => c * 9 / 5 + 32);

        Assert.That(celsius.Value, Is.EqualTo(100));

        celsius.Value = 0;
        Assert.That(fahrenheit.Value, Is.EqualTo(32));
    }

    [Test]
    public void TwoWay_NonInverseTransforms_MakeExactlyOneTrip()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        var aToB = 0;
        var bToA = 0;
        a.BindTwoWayFrom(
            b,
            fromOther: v => { bToA++; return v * 2; },
            toOther: v => { aToB++; return v + 1; });
        aToB = 0;
        bToA = 0;

        b.Value = 4;

        Assert.That(a.Value, Is.EqualTo(8));
        Assert.That(b.Value, Is.EqualTo(4));
        Assert.That(bToA, Is.EqualTo(1));
        Assert.That(aToB, Is.EqualTo(0));
    }

    [Test]
    public void TwoWay_RequiresBothSlotsFree()
    {
        var source = new Property<int>(0);
        var bound = new Property<int>(0);
        bound.BindFrom(source);
        var free = new Property<int>(0);

        Assert.Throws<InvalidOperationException>(() => bound.BindTwoWayFrom(free));
        Assert.Throws<InvalidOperationException>(() => free.BindTwoWayFrom(bound));
    }

    [Test]
    public void TwoWay_Self_Throws()
    {
        var cell = new Property<int>(0);

        Assert.Throws<InvalidOperationException>(() => cell.BindTwoWayFrom(cell));
    }

    [Test]
    public void BindFrom_OnTwoWayEnd_Throws()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.BindTwoWayFrom(b);
        var other = new Property<int>(0);

        Assert.Throws<InvalidOperationException>(() => a.BindFrom(other));
        Assert.Throws<InvalidOperationException>(() => b.BindFrom(other));
    }

    [Test]
    public void Unbind_EitherEnd_ReleasesBoth()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.BindTwoWayFrom(b);

        b.Unbind();
        a.Value = 1;
        b.Value = 2;

        Assert.That(a.IsBound, Is.False);
        Assert.That(b.IsBound, Is.False);
        Assert.That(a.Value, Is.EqualTo(1));
        Assert.That(b.Value, Is.EqualTo(2));
    }

    [Test]
    public void ThirdParty_CanListenOnTwoWayEnd()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.BindTwoWayFrom(b);
        var mirror = new Property<int>(0);
        mirror.BindFrom(a);

        b.Value = 7;

        Assert.That(mirror.Value, Is.EqualTo(7));
    }

    [Test]
    public void TwoWayPair_DoesNotTrapCycleWalk()
    {
        var a = new Property<int>(0);
        var b = new Property<int>(0);
        a.BindTwoWayFrom(b);
        var observer = new Property<int>(0);

        observer.BindFrom(a);
        b.Value = 3;

        Assert.That(observer.Value, Is.EqualTo(3));
    }
}
