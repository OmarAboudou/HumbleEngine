namespace HumbleEngine.Core;

public abstract class Node
{
    public Node? Parent { get; private set; }
    private List<Node> _children = [];
    public IReadOnlyList<Node> Children;
    
    public Node()
    {
        Children = _children.AsReadOnly();
    }
    
    public void AddChild(Node node)
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

    public void RemoveChild(Node node)
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
    
}