namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; private set; }

    public NodeTree(Node root)
    {
        Root = root;
        root.Tree = this;
    }

    public void Process(double delta) => ExecuteInPrefixOrder(node => node.Process(delta));
    public void PhysicsProcess(double delta) => ExecuteInPrefixOrder(node => node.PhysicsProcess(delta));

    private void ExecuteInPrefixOrder(Action<Node> action)
    {
        List<Node> nodesInPrefixOrder = [Root];
        while (nodesInPrefixOrder.Count > 0)
        {
            Node current = nodesInPrefixOrder[0];
            action(current);
            nodesInPrefixOrder.RemoveAt(0);
            nodesInPrefixOrder.AddRange(current.Children);
        }
    }
}