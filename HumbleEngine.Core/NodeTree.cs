namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; }
    public NodeTree(Node root)
    {
        Root = root;
        RegisterSubtree(Root);
    }
    public void Shutdown()
    {
        UnregisterSubtree(Root);
    }
    
    #region Node Tree Commands

    private readonly Queue<NodeTreeCommand> _commands = new();
    public void QueueCommand(NodeTreeCommand command)
    {
        if (this._commands.Contains(command))
        {
            Console.WriteLine($"The command {command} is already queued.");
            return;
        }
        Console.WriteLine($"Queueing command {command}.");
        this._commands.Enqueue(command);
    }
    private void FlushCommands()
    {
        int count = this._commands.Count;
        for (int i = 0; i < count; i++)
        {
            this._commands.Dequeue().Execute(this);
        }
    }

    #endregion

    #region Subtree Registration

    internal void RegisterSubtree(Node root)
    {
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Tree = this;
        });
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.TreeEntered();
        });
    }

    internal void UnregisterSubtree(Node root)
    {
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.TreeExiting();
        });
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Tree = null;
        });
    }

    #endregion

    #region Tree Processing

    public void Process(double delta)
    {
        this.GetNodesInPrefixOrder().ForEach(node => node.Process(delta));
        this.FlushCommands();
    }
    public void PhysicsProcess(double delta)
    {
        this.GetNodesInPrefixOrder().ForEach(node => node.PhysicsProcess(delta));
        this.FlushCommands();
    }

    #endregion

    #region Utils

    public IEnumerable<Node> GetNodesInPrefixOrder() => Root.GetSubtreeInPrefixOrder();

    #endregion
    
}