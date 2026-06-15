namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the flex inline properties and descriptors: every
/// <see cref="UINode"/> carries <see cref="UINode.FlexFactor"/>/<see cref="UINode.FlexTight"/>,
/// and the <see cref="Expanded"/>/<see cref="Flexible"/> descriptors set them at
/// <c>Add</c> (no extra node).
/// </summary>
public sealed class FlexTests
{
    [Test]
    public void PlainChild_IsNotFlex()
    {
        var row   = new Row();
        var child = new Panel();

        row.Children.Add(child);

        Assert.That(child.FlexFactor.Value, Is.EqualTo(0f)); // 0 = content-sized
    }

    [Test]
    public void Expanded_SetsFactorAndTightFit()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Expanded(child, factor: 2f));

        Assert.That(child.Parent, Is.SameAs(row));
        Assert.That(child.FlexFactor.Value, Is.EqualTo(2f));
        Assert.That(child.FlexTight.Value,  Is.True);
    }

    [Test]
    public void Flexible_SetsFactorAndLooseFit()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Flexible(child, factor: 3f));

        Assert.That(child.FlexFactor.Value, Is.EqualTo(3f));
        Assert.That(child.FlexTight.Value,  Is.False);
    }

    [Test]
    public void Expanded_DefaultFactorIsOne()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Expanded(child));

        Assert.That(child.FlexFactor.Value, Is.EqualTo(1f));
    }

    [Test]
    public void RawFactor_SetDirectly_IsLoose()
    {
        var child = new Panel();

        child.FlexFactor.Value = 1f; // no descriptor

        Assert.That(child.FlexTight.Value, Is.False); // a raw factor is loose by default
    }

    [Test]
    public void Add_UINode_IsSugarForChildrenAdd()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(child);

        Assert.That(child.Parent, Is.SameAs(row));
        Assert.That(child.FlexFactor.Value, Is.EqualTo(0f));
    }

    [Test]
    public void Expanded_RequiresAChild()
    {
        Assert.That(() => new Expanded(null!), Throws.ArgumentNullException);
    }
}
