namespace HumbleEngine.Core;

/// <summary>
/// Command that removes <see cref="Child"/> from <see cref="Parent"/> inside a <see cref="NodeTree"/>.
/// The child's entire subtree is unregistered from the tree before detaching.
/// </summary>
/// <param name="Parent">The node from which the child will be removed.</param>
/// <param name="Child">The node to detach.</param>
public record RemoveChildCommand(Node Parent, Node Child) : NodeTreeCommand
{
    /// <inheritdoc />
    public void Execute(NodeTree tree)
    {
        if (Parent.Tree == null)
            throw new InvalidOperationException($"The parent node {Parent} is not inside a tree.");
        if (Parent.Tree != tree)
            throw new InvalidOperationException($"The parent node {Parent} is not inside the tree {tree}.");
        if (Child.Tree != tree)
            throw new InvalidOperationException($"The child node {Child} is not inside the tree {tree}.");

        tree.UnregisterSubtree(Child);
        Parent.RemoveChildRightAway(Child);
        Child.GetSubtreeInPrefixOrder().ForEach(node => tree._onNodeRemoved.Emit(node));
        tree._onTreeChanged.Emit();
    }
}