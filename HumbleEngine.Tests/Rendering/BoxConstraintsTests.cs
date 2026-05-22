namespace HumbleEngine.Tests;

[TestFixture]
public class BoxConstraintsTests
{
    [Test]
    public void Loose_MaxEqualsAvailableSize()
    {
        var c = BoxConstraints.Loose(new Size(800, 600));
        Assert.That(c.MinWidth,  Is.EqualTo(0));
        Assert.That(c.MaxWidth,  Is.EqualTo(800));
        Assert.That(c.MinHeight, Is.EqualTo(0));
        Assert.That(c.MaxHeight, Is.EqualTo(600));
    }

    [Test]
    public void Tight_MinEqualsMax()
    {
        var c = BoxConstraints.Tight(new Size(100, 50));
        Assert.That(c.MinWidth,  Is.EqualTo(100));
        Assert.That(c.MaxWidth,  Is.EqualTo(100));
        Assert.That(c.MinHeight, Is.EqualTo(50));
        Assert.That(c.MaxHeight, Is.EqualTo(50));
    }

    [Test]
    public void Unconstrained_MaxIsInfinity()
    {
        var c = BoxConstraints.Unconstrained;
        Assert.That(c.MinWidth,  Is.EqualTo(0));
        Assert.That(c.MaxWidth,  Is.EqualTo(float.PositiveInfinity));
        Assert.That(c.MinHeight, Is.EqualTo(0));
        Assert.That(c.MaxHeight, Is.EqualTo(float.PositiveInfinity));
    }

    [Test]
    public void Constrain_ValueWithinBounds_Unchanged()
    {
        var c = BoxConstraints.Loose(new Size(800, 600));
        var s = c.Constrain(300, 200);
        Assert.That(s.Width,  Is.EqualTo(300));
        Assert.That(s.Height, Is.EqualTo(200));
    }

    [Test]
    public void Constrain_ValueOverMax_ClampsToMax()
    {
        var c = BoxConstraints.Loose(new Size(200, 100));
        var s = c.Constrain(500, 300);
        Assert.That(s.Width,  Is.EqualTo(200));
        Assert.That(s.Height, Is.EqualTo(100));
    }

    [Test]
    public void Constrain_ValueUnderMin_ClampsToMin()
    {
        var c = BoxConstraints.Tight(new Size(100, 50));
        var s = c.Constrain(0, 0);
        Assert.That(s.Width,  Is.EqualTo(100));
        Assert.That(s.Height, Is.EqualTo(50));
    }
}
