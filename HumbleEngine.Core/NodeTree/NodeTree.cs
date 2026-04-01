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
/// <para>
/// The following table summarizes the order in which lifecycle callbacks are invoked
/// when a subtree is registered or unregistered:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Callback</term>
///     <description>Order</description>
///   </listheader>
///   <item>
///     <term><see cref="Node.TreeEntered"/></term>
///     <description>Prefix — parent before children</description>
///   </item>
///   <item>
///     <term><see cref="Node.OnReady"/></term>
///     <description>Reverse prefix — children before parent</description>
///   </item>
///   <item>
///     <term><see cref="Node.Unready"/></term>
///     <description>Prefix — parent before children</description>
///   </item>
///   <item>
///     <term><see cref="Node.TreeExiting"/></term>
///     <description>Reverse prefix — children before parent</description>
///   </item>
/// </list>
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
        if (_commands.Contains(command))
        {
            Services.Logger.Warning<NodeTreeChannel>($"The command {command} is already queued and will not be queued once again.");
            return;
        }
        Services.Logger.Trace<NodeTreeChannel>($"Queueing command {command}.");
        _commands.Enqueue(command);
    }

    /// <summary>
    /// Executes all queued commands in FIFO order, then clears the queue.
    /// Only the commands present at the time of the call are executed;
    /// commands enqueued during execution are left for the next flush.
    /// </summary>
    private void FlushCommands()
    {
        int count = _commands.Count;
        for (int i = 0; i < count; i++)
        {
            _commands.Dequeue().Execute(this);
        }
    }

    #endregion

    #region Subtree Registration

    /// <summary>
    /// Registers all nodes in <paramref name="root"/>'s subtree into this tree.
    /// Proceeds in three passes:
    /// <list type="number">
    ///   <item><description>Sets <see cref="Node.Tree"/> on all nodes in prefix order.</description></item>
    ///   <item><description>Calls <see cref="Node.TreeEntered"/> on all nodes in prefix order (parent before children).</description></item>
    ///   <item><description>Calls <see cref="Node.OnReady"/> on all nodes in reverse prefix order (children before parent).</description></item>
    /// </list>
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
            node.Emit(node.OnTreeEntered);
        });
        root.GetSubtreeInReversePrefixOrder().ForEach(node =>
        {
            node.Ready();
            node.Emit(node.OnReady);
        });
    }

    /// <summary>
    /// Unregisters all nodes in <paramref name="root"/>'s subtree from this tree.
    /// Proceeds in three passes:
    /// <list type="number">
    ///   <item><description>Calls <see cref="Node.Unready"/> on all nodes in prefix order (parent before children).</description></item>
    ///   <item><description>Calls <see cref="Node.TreeExiting"/> on all nodes in reverse prefix order (children before parent).</description></item>
    ///   <item><description>Sets <see cref="Node.Tree"/> to <c>null</c> on all nodes in reverse prefix order.</description></item>
    /// </list>
    /// </summary>
    /// <param name="root">The root of the subtree to unregister.</param>
    internal void UnregisterSubtree(Node root)
    {
        root.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Unready();
            node.Emit(node.OnUnready);
        });
        root.GetSubtreeInReversePrefixOrder().ForEach(node =>
        {
            node.TreeExiting();
            node.Emit(node.OnTreeExiting);
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
        GetNodesInPrefixOrder().ForEach(node => node.Process(delta));
        FlushCommands();
    }

    /// <summary>
    /// Runs one physics tick: calls <see cref="Node.PhysicsProcess"/> on every node in prefix order,
    /// then flushes all queued commands.
    /// </summary>
    /// <param name="delta">The elapsed time in seconds since the previous physics tick.</param>
    public void PhysicsProcess(double delta)
    {
        GetNodesInPrefixOrder().ForEach(node => node.PhysicsProcess(delta));
        FlushCommands();
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