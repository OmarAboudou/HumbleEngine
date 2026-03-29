namespace HumbleEngine.Core;

/// <summary>
/// Represents a running node tree for an application. An <see cref="NodeTree"/> owns a
/// <see cref="Root"/> node and manages the game loop (process, physics) and deferred
/// structural commands for every node in the tree.
/// </summary>
/// <remarks>
/// Structural changes (adding/removing children) that occur while nodes are attached to the tree
/// are queued as <see cref="NodeTreeCommand"/> instances and executed at the end of each
/// <see cref="Process"/> or <see cref="PhysicsProcess"/> call via <see cref="FlushCommands"/>.
/// </remarks>
public class NodeTree
{
    /// <summary>
    /// The root node of this tree. Set once at construction and never changes.
    /// </summary>
    public Node Root { get; }

    /// <summary>
    /// Creates a new <see cref="NodeTree"/> with the given root node.
    /// All nodes already present in the root's subtree are registered into the tree.
    /// </summary>
    /// <param name="root">The node that will serve as the root of this tree.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="root"/> already has a parent.</exception>
    public NodeTree(Node root)
    {
        if (root.Parent != null)
            throw new ArgumentException($"The node {root} cannot be the root of a tree because it already has a parent.", nameof(root));

        Root = root;
        RegisterSubtree(Root);
    }
    
    #region Node Tree Commands

    private readonly Queue<NodeTreeCommand> _commands = new();

    /// <summary>
    /// Enqueues a structural command to be executed at the next <see cref="FlushCommands"/> call.
    /// Duplicate commands are silently ignored.
    /// </summary>
    /// <param name="command">The command to enqueue.</param>
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

    /// <summary>
    /// Executes all queued commands in FIFO order, then clears the queue.
    /// Only the commands present at the time of the call are executed;
    /// commands enqueued during execution are left for the next flush.
    /// </summary>
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

    /// <summary>
    /// Registers all nodes in <paramref name="root"/>'s subtree into this tree.
    /// Sets their <see cref="Node.Tree"/> property, then calls <see cref="Node.TreeEntered"/>
    /// on each node in prefix order.
    /// </summary>
    /// <param name="root">The root of the subtree to register.</param>
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
        root.GetSubtreeInReversePrefixOrder().ForEach(node =>
        {
            node.Ready();
        });
    }

    /// <summary>
    /// Unregisters all nodes in <paramref name="root"/>'s subtree from this tree.
    /// Calls <see cref="Node.TreeExiting"/> on each node in reverse prefix order (children before
    /// their parent), then sets their <see cref="Node.Tree"/> property to <c>null</c>.
    /// </summary>
    /// <param name="root">The root of the subtree to unregister.</param>
    internal void UnregisterSubtree(Node root)
    {
        root.GetSubtreeInReversePrefixOrder().ForEach(node =>
        {
            node.TreeExiting();
        });
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Unready();
        });
        root.GetSubtreeInReversePrefixOrder().ForEach(node =>
        {
            node.Tree = null;
        });
    }

    #endregion

    #region Tree Processing

    /// <summary>
    /// Runs one frame tick: calls <see cref="Node.Process"/> on every node in prefix order,
    /// then flushes all queued commands.
    /// </summary>
    /// <param name="delta">The elapsed time in seconds since the previous frame.</param>
    public void Process(double delta)
    {
        this.GetNodesInPrefixOrder().ForEach(node => node.Process(delta));
        this.FlushCommands();
    }

    /// <summary>
    /// Runs one physics tick: calls <see cref="Node.PhysicsProcess"/> on every node in prefix order,
    /// then flushes all queued commands.
    /// </summary>
    /// <param name="delta">The elapsed time in seconds since the previous physics tick.</param>
    public void PhysicsProcess(double delta)
    {
        this.GetNodesInPrefixOrder().ForEach(node => node.PhysicsProcess(delta));
        this.FlushCommands();
    }

    #endregion

    #region Utils

    /// <summary>
    /// Enumerates every node in the tree in depth-first prefix order, starting from the root.
    /// </summary>
    /// <returns>An enumerable of all nodes in the tree.</returns>
    public IEnumerable<Node> GetNodesInPrefixOrder() => Root.GetSubtreeInPrefixOrder();

    #endregion
    
}