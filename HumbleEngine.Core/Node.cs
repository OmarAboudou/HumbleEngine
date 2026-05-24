using System.Collections;

namespace HumbleEngine.Core;

public abstract class Node : IEnumerable<Node>, IDisposable
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

    #endregion
    
    public IEnumerator<Node> GetEnumerator() 
        => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public void Dispose()
    {
        _children.ForEach(x => x.Dispose());
    }
}