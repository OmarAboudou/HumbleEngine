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
        {
            Console.Error.WriteLine($"The parent node {Parent} is not inside a tree.");
            return;
        }
        
        if (Parent.Tree != tree)
        {
            Console.Error.WriteLine($"The parent node {Parent} is not inside the tree {tree}.");
            return;
        }

        if (Child.Tree != tree)
        {
            Console.Error.WriteLine($"The child node {Child} is not inside the tree {tree}.");
            return;
        }

        if(!Parent.CanRemoveChild(Child)) return;
        
        tree.UnregisterSubtree(Child);
        Parent.RemoveChildRightAway(Child);

    }
}