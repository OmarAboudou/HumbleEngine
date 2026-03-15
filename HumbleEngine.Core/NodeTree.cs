namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; private set; }

    public NodeTree(Node root)
    {
        this.Root = root;
        root.NodeTree = this;
    }
}