namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; private set; }
    public NodeTree(Node root)
    {
        Root = root;
        root.Tree = this;
    }

    public void Process(double delta) => this.GetNodesInPrefixOrder().ForEach(node => node.Process(delta));
    public void PhysicsProcess(double delta) => this.GetNodesInPrefixOrder().ForEach(node => node.PhysicsProcess(delta));

    public IEnumerable<Node> GetNodesInPrefixOrder()
    {
        Stack<Node> nodeStack = new();
        nodeStack.Push(Root);
        
        while (nodeStack.Count > 0)
        {
            Node current = nodeStack.Pop();
            yield return current;
            
            for (int i = current.Children.Count - 1; i >= 0; i--)
            {
                nodeStack.Push(current.Children[i]);
            }
        }
    }
    
}