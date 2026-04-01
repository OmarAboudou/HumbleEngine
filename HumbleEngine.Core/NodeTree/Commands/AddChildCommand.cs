namespace HumbleEngine.Core;

/// <summary>
/// Command that adds <see cref="Child"/> as a child of <see cref="Parent"/> inside a <see cref="NodeTree"/>.
/// After attaching the child, the child's entire subtree is registered into the tree.
/// </summary>
/// <param name="Parent">The node that will receive the new child.</param>
/// <param name="Child">The node to attach as a child.</param>
public record AddChildCommand(Node Parent, Node Child) : NodeTreeCommand
{
    /// <inheritdoc />
    public void Execute(NodeTree tree)
    {
        if (Parent.Tree == null)
            throw new InvalidOperationException($"The parent node {Parent} is not inside a tree.");
        if (Parent.Tree != tree)
            throw new InvalidOperationException($"The parent node {Parent} is not inside the tree {tree}.");

        Parent.AddChildRightAway(Child);
        tree.RegisterSubtree(Child);
        Child.GetSubtreeInPrefixOrder().ForEach(node => tree.Emit(tree.OnNodeAdded, node));
        tree.Emit(tree.OnTreeChanged);
    }
}