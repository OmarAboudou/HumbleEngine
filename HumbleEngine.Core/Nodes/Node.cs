using System.Collections;

namespace HumbleEngine.Core;

public class Node : HumbleObject, IEnumerable<Node>
{
    public Node()
    {
        Children = _children.AsReadOnly();
    }

    #region Tree Structure

    public Node? Parent { get; private set; }
    
    private readonly List<Node> _children = [];
    
    public readonly IReadOnlyList<Node> Children;

    public void Add(Node node)
    {
        if (node.Parent == null)
        {
            node.Parent = this;
            _children.Add(node);
            return;
        }
        
        if (node == this)
        {
            throw new Exception($"A {nameof(Node)} cannot be its own child.");
        }
        else if (node.Parent == this)
        {
            Console.WriteLine($"{nameof(Node)}( {node} ) is already a child of {nameof(Node)}( {this} )");
        }
        else if (node.Parent != null)
        {
            throw new Exception($"{nameof(Node)}( {node} ) must have no parent when being added as a child of {nameof(Node)}( {this} )");
        }
    }

    public void Remove(Node node)
    {
        if (node.Parent == this)
        {
            _children.Remove(node);
            node.Parent = null;
            return;
        }

        if (node.Parent != this)
        {
            Console.WriteLine($"{nameof(Node)}( {node} ) cannot be removed from the children of  {nameof(Node)}( {this} ) because it is not a child of {nameof(Node)}( {this} ).");
        }
        
    }

    public IEnumerable<Node> GetSubtreeInDepthFirstOrder()
    {
        Stack<Node> nodeStack = new([this]);
        while (nodeStack.Count > 0)
        {
            Node node = nodeStack.Pop();
            yield return node;
            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                nodeStack.Push(node.Children[i]);
            }
        }
    }

    public IEnumerable<Node> GetSubtreeInReverseDepthFirstOrder()
        => GetSubtreeInDepthFirstOrder().Reverse();

    public IEnumerable<Node> GetAncestorsClosestToFarthest()
    {
        Node? Ancestor = this.Parent;
        while (Ancestor != null)
        {
            yield return Ancestor;
            Ancestor = Ancestor.Parent;
        }
    }
    
    #endregion
    
    public IEnumerator<Node> GetEnumerator() 
        => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public override void Dispose()
    {
        base.Dispose();
        _children.ForEach(x => x.Dispose());
        _children.Clear();
    }
}