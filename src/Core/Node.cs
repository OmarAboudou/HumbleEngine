namespace HumbleEngine.Core;

public class Node
{
    internal Node? Parent {get; private set;}
    
    public IReadOnlyList<Node> Children {get;}
    
    private readonly List<Node> children = [];

    public Node() => Children = children.AsReadOnly();
    

    public bool IsDescendantOf(Node other)
    {
        Node? current = Parent;
        while(current is not null)
        {
            if(current == other)
                return true;
            
            current = current.Parent;
        }
        return false;
    }

    public bool IsAncestorOf(Node other) => other.IsDescendantOf(this);

    public void AddChildAt(int index, Node child)
    {
        ArgumentNullException.ThrowIfNull(child, nameof(child));
        
        if(ReferenceEquals(this, child) || child.IsAncestorOf(this))
            throw new NodeCycleException(this, child);

        if(child.Parent is not null)
            throw new NodeAlreadyHasAParentException(this, child);

        children.Insert(index, child);
        child.Parent = this;
    }

    public void RemoveChildAt(int index)
    {
        children[index].Parent = null;
        children.RemoveAt(index);
    }

}
