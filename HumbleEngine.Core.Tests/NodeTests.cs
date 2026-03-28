namespace HumbleEngine.Core.Tests;

[TestFixture]
public class NodeTests
{
    [Test]
    public void AddChild_DetachedNode_SetsParentAndAddsToChildren()
    {
        var parent = new Node();
        var child = new Node();

        parent.AddChild(child);

        Assert.That(child.Parent, Is.EqualTo(parent));
        Assert.That(parent.Children, Contains.Item(child));
    }

    [Test]
    public void AddChild_NodeToItself_ThrowsArgumentException()
    {
        var node = new Node();

        Assert.Throws<ArgumentException>(() => node.AddChild(node));
    }

    [Test]
    public void AddChild_ChildWithExistingParent_ThrowsInvalidOperationException()
    {
        var parent1 = new Node();
        var parent2 = new Node();
        var child = new Node();
        parent1.AddChild(child);

        Assert.Throws<InvalidOperationException>(() => parent2.AddChild(child));
    }

    [Test]
    public void AddChild_ChildAlreadyInTree_ThrowsInvalidOperationException()
    {
        var root = new Node();
        var child = new Node();
        var tree = new NodeTree(root);
        root.AddChild(child);
        tree.Process(0);

        var otherParent = new Node();
        Assert.Throws<InvalidOperationException>(() => otherParent.AddChild(child));
    }

    [Test]
    public void AddChild_ChildAlreadyInChildren_ThrowsInvalidOperationException()
    {
        var parent = new Node();
        var child = new Node();
        parent.AddChild(child);

        Assert.Throws<InvalidOperationException>(() => parent.AddChild(child));
    }

    [Test]
    public void TryAddChild_DetachedNode_ReturnsTrueAndSetsParent()
    {
        var parent = new Node();
        var child = new Node();

        bool result = parent.TryAddChild(child);

        Assert.That(result, Is.True);
        Assert.That(child.Parent, Is.EqualTo(parent));
        Assert.That(parent.Children, Contains.Item(child));
    }

    [Test]
    public void TryAddChild_NodeToItself_ReturnsFalse()
    {
        var node = new Node();

        bool result = node.TryAddChild(node);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryAddChild_ChildWithExistingParent_ReturnsFalse()
    {
        var parent1 = new Node();
        var parent2 = new Node();
        var child = new Node();
        parent1.AddChild(child);

        bool result = parent2.TryAddChild(child);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryAddChild_ChildAlreadyInTree_ReturnsFalse()
    {
        var root = new Node();
        var child = new Node();
        var tree = new NodeTree(root);
        root.AddChild(child);
        tree.Process(0);

        var otherParent = new Node();
        bool result = otherParent.TryAddChild(child);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryAddChild_ChildAlreadyInChildren_ReturnsFalse()
    {
        var parent = new Node();
        var child = new Node();
        parent.AddChild(child);

        bool result = parent.TryAddChild(child);

        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveChild_DetachedNode_ClearsParentAndRemovesFromChildren()
    {
        var parent = new Node();
        var child = new Node();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(child.Parent, Is.Null);
        Assert.That(parent.Children, Does.Not.Contain(child));
    }

    [Test]
    public void RemoveChild_NodeFromItself_ThrowsArgumentException()
    {
        var node = new Node();

        Assert.Throws<ArgumentException>(() => node.RemoveChild(node));
    }

    [Test]
    public void RemoveChild_ChildWithNoParent_ThrowsInvalidOperationException()
    {
        var parent = new Node();
        var child = new Node();

        Assert.Throws<InvalidOperationException>(() => parent.RemoveChild(child));
    }

    [Test]
    public void RemoveChild_ChildWithDifferentParent_ThrowsInvalidOperationException()
    {
        var parent1 = new Node();
        var parent2 = new Node();
        var child = new Node();
        parent1.AddChild(child);

        Assert.Throws<InvalidOperationException>(() => parent2.RemoveChild(child));
    }

    [Test]
    public void TryRemoveChild_ValidChild_ReturnsTrueAndClearsParent()
    {
        var parent = new Node();
        var child = new Node();
        parent.AddChild(child);

        bool result = parent.TryRemoveChild(child);

        Assert.That(result, Is.True);
        Assert.That(child.Parent, Is.Null);
        Assert.That(parent.Children, Does.Not.Contain(child));
    }

    [Test]
    public void TryRemoveChild_NodeFromItself_ReturnsFalse()
    {
        var node = new Node();

        bool result = node.TryRemoveChild(node);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryRemoveChild_ChildWithNoParent_ReturnsFalse()
    {
        var parent = new Node();
        var child = new Node();

        bool result = parent.TryRemoveChild(child);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryRemoveChild_ChildWithDifferentParent_ReturnsFalse()
    {
        var parent1 = new Node();
        var parent2 = new Node();
        var child = new Node();
        parent1.AddChild(child);

        bool result = parent2.TryRemoveChild(child);

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetSubtreeInPrefixOrder_SingleNode_ReturnsSelf()
    {
        var node = new Node();

        var result = node.GetSubtreeInPrefixOrder();

        Assert.That(result, Is.EqualTo(new[] { node }));
    }

    [Test]
    public void GetSubtreeInPrefixOrder_MultiLevelTree_ReturnsNodesInPrefixOrder()
    {
        //     root
        //    /    \
        //   a      b
        //  / \
        // c   d
        var root = new Node();
        var a = new Node();
        var b = new Node();
        var c = new Node();
        var d = new Node();
        root.AddChild(a);
        root.AddChild(b);
        a.AddChild(c);
        a.AddChild(d);

        var result = root.GetSubtreeInPrefixOrder();

        Assert.That(result, Is.EqualTo(new[] { root, a, c, d, b }));
    }
}