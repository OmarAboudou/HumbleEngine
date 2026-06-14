namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the flex descriptors and parent-data: a <see cref="LinearContainer"/>
/// stamps a <see cref="FlexParentData"/> on every child, and the <see cref="Expanded"/>/
/// <see cref="Flexible"/> descriptors dissolve into it at <c>Add</c> (no extra node).
/// </summary>
public sealed class FlexTests
{
    private static FlexParentData Flex(UINode node) => (FlexParentData)node.ParentData!;

    [Test]
    public void PlainChild_IsNotFlex()
    {
        var row   = new Row();
        var child = new Panel();

        row.Children.Add(child);

        Assert.That(child.ParentData, Is.InstanceOf<FlexParentData>());
        Assert.That(Flex(child).Factor.Value, Is.EqualTo(0f)); // 0 = content-sized
    }

    [Test]
    public void Expanded_SetsFactorAndTightFit()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Expanded(child, factor: 2f));

        Assert.That(child.Parent, Is.SameAs(row));
        Assert.That(Flex(child).Factor.Value, Is.EqualTo(2f));
        Assert.That(Flex(child).Tight.Value,  Is.True);
    }

    [Test]
    public void Flexible_SetsFactorAndLooseFit()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Flexible(child, factor: 3f));

        Assert.That(Flex(child).Factor.Value, Is.EqualTo(3f));
        Assert.That(Flex(child).Tight.Value,  Is.False);
    }

    [Test]
    public void Expanded_DefaultFactorIsOne()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(new Expanded(child));

        Assert.That(Flex(child).Factor.Value, Is.EqualTo(1f));
    }

    [Test]
    public void Add_UINode_IsSugarForChildrenAdd()
    {
        var row   = new Row();
        var child = new Panel();

        row.Add(child);

        Assert.That(child.Parent, Is.SameAs(row));
        Assert.That(Flex(child).Factor.Value, Is.EqualTo(0f));
    }

    [Test]
    public void Expanded_RequiresAChild()
    {
        Assert.That(() => new Expanded(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void LeavingTheContainer_ClearsTheFlexData()
    {
        var row   = new Row();
        var child = new Panel();
        row.Add(new Expanded(child));

        row.Children.Remove(child);

        Assert.That(child.ParentData, Is.Null);
    }
}
