namespace HumbleEngine.Core;

public class NodeTree
{
    public Node Root { get; }
    public NodeTree(Node root)
    {
        Root = root;
        root.Tree = this;
    }

    #region Node Tree Commands

    private readonly Queue<NodeTreeCommand> _commands = new();
    public void QueueCommand(NodeTreeCommand command)
    {
        if (this._commands.Contains(command))
        {
            // TODO : Use log system
            Console.WriteLine($"The command {command} is already queued.");
            return;
        }
        Console.WriteLine($"Queueing command {command}.");
        this._commands.Enqueue(command);
    }
    private void FlushCommands()
    {
        this._commands.ForEach(ExecuteCommand);
    }
    private void ExecuteCommand(NodeTreeCommand command) => command.Execute(this);    

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