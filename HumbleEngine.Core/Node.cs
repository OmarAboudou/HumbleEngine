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
    
    
}