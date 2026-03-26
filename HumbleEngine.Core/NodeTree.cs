namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; private set; }

    public NodeTree(Node root)
    {
        Root = root;
        root.Tree = this;
    }
    
}