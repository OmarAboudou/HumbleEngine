using System.Collections;

namespace HumbleEngine.Core;

public class Node : HumbleObject, IEnumerable<Node>
{
    public Node()
    {
        Parent = CreateProtectedProperty(null, out _parent);
        Children = CreateProtectedListProperty([], out _children);
    }

    public IEnumerator<Node> GetEnumerator() 
        => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public override void Dispose()
    {
        base.Dispose();
        _children.ToList().ForEach(x => x.Dispose());
        _children.Clear();
    }

    #region Tree Structure

    public readonly IPropertyListener<Node?> Parent;
    private readonly Property<Node?> _parent;

    public readonly IListPropertyListener<Node> Children;
    private readonly ListProperty<Node> _children;

    public void Add(Node node)
    {
        if (node._parent.Value == null)
        {
            node._parent.Value = this;
            _children.Add(node);
            return;
        }

        if (node == this) throw new Exception($"A {nameof(Node)} cannot be its own child.");

        if (node._parent.Value == this)
            Console.WriteLine($"{nameof(Node)}( {node} ) is already a child of {nameof(Node)}( {this} )");
        else if (node._parent.Value != null)
            throw new Exception(
                $"{nameof(Node)}( {node} ) must have no parent when being added as a child of {nameof(Node)}( {this} )");
    }

    public void Add(IReadOnlyList<Node> nodes)
    {
        foreach (Node node in nodes)
            Add(node);
    }

    public void Remove(Node node)
    {
        if (node._parent.Value == this)
        {
            _children.Remove(node);
            node._parent.Value = null;
            return;
        }

        if (node._parent.Value != this)
            Console.WriteLine(
                $"{nameof(Node)}( {node} ) cannot be removed from the children of  {nameof(Node)}( {this} ) because it is not a child of {nameof(Node)}( {this} ).");
    }

    public IEnumerable<Node> GetSubtreeInDepthFirstOrder()
    {
        Stack<Node> nodeStack = new([this]);
        while (nodeStack.Count > 0)
        {
            Node node = nodeStack.Pop();
            yield return node;
            for (int i = node.Children.Count - 1; i >= 0; i--) nodeStack.Push(node.Children[i]);
        }
    }

    public IEnumerable<Node> GetSubtreeInReverseDepthFirstOrder() 
        => GetSubtreeInDepthFirstOrder().Reverse();

    public IEnumerable<Node> GetAncestorsClosestToFarthest()
    {
        Node? Ancestor = _parent.Value;
        while (Ancestor != null)
        {
            yield return Ancestor;
            Ancestor = Ancestor._parent.Value;
        }
    }

    #endregion
}