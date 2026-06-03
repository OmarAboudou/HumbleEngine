namespace HumbleEngine.Core;

public class Node
{
    public Node()
    {
        Children = _children.AsReadOnly();
    }

    public Node? Parent { get; private set; }
    private List<Node> _children = [];
    public IReadOnlyList<Node> Children;
}