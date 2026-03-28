namespace HumbleEngine.Core;

/// <summary>
/// Represents a deferred structural command on the node tree (e.g. adding or removing a child).
/// Commands are queued via <see cref="NodeTree.QueueCommand"/> and executed during
/// <see cref="NodeTree.Process"/> or <see cref="NodeTree.PhysicsProcess"/>.
/// </summary>
public interface NodeTreeCommand
{
    /// <summary>
    /// Executes this command against the given <paramref name="tree"/>.
    /// </summary>
    /// <param name="tree">The tree in which the command should be applied.</param>
    public void Execute(NodeTree tree);
}