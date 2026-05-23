using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class NodeTraversalTests
{
    private sealed class TestNode(string name) : Node
    {
        public override string ToString() => name;
    }

    private static TestNode Tree(out TestNode b, out TestNode c, out TestNode d)
    {
        //      A
        //     / \
        //    B   C
        //    |
        //    D
        var a = new TestNode("A");
        b = new TestNode("B");
        c = new TestNode("C");
        d = new TestNode("D");
        a.Attach(b);
        a.Attach(c);
        b.Attach(d);
        return a;
    }

    #region GetSubtreeDepthFirst

    [Test]
    public void DepthFirst_SingleNode_ReturnsSelf()
    {
        var node = new TestNode("A");
        Assert.That(node.GetSubtreeDepthFirst(), Is.EqualTo(new[] { node }));
    }

    [Test]
    public void DepthFirst_Tree_ReturnsPreOrder()
    {
        var a = Tree(out var b, out var c, out var d);
        Assert.That(a.GetSubtreeDepthFirst(), Is.EqualTo(new Node[] { a, b, d, c }));
    }

    [Test]
    public void DepthFirst_FromSubtree_DoesNotIncludeRoot()
    {
        Tree(out var b, out _, out var d);
        Assert.That(b.GetSubtreeDepthFirst(), Is.EqualTo(new Node[] { b, d }));
    }

    [Test]
    public void DepthFirst_Leaf_ReturnsSelf()
    {
        Tree(out _, out var c, out _);
        Assert.That(c.GetSubtreeDepthFirst(), Is.EqualTo(new[] { c }));
    }

    #endregion

    #region GetSubtreeReverseDepthFirst

    [Test]
    public void ReverseDepthFirst_SingleNode_ReturnsSelf()
    {
        var node = new TestNode("A");
        Assert.That(node.GetSubtreeReverseDepthFirst(), Is.EqualTo(new[] { node }));
    }

    [Test]
    public void ReverseDepthFirst_Tree_ReturnsReversedPreOrder()
    {
        var a = Tree(out var b, out var c, out var d);
        Assert.That(a.GetSubtreeReverseDepthFirst(), Is.EqualTo(new Node[] { c, d, b, a }));
    }

    [Test]
    public void ReverseDepthFirst_FromSubtree_DoesNotIncludeRoot()
    {
        Tree(out var b, out _, out var d);
        Assert.That(b.GetSubtreeReverseDepthFirst(), Is.EqualTo(new Node[] { d, b }));
    }

    [Test]
    public void ReverseDepthFirst_Leaf_ReturnsSelf()
    {
        Tree(out _, out var c, out _);
        Assert.That(c.GetSubtreeReverseDepthFirst(), Is.EqualTo(new[] { c }));
    }

    #endregion
}
