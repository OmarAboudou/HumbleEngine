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
        {
            Console.Error.WriteLine($"The parent node {Parent} is not inside a tree.");
            return;
        }
        
        if (Parent.Tree != tree)
        {
            Console.Error.WriteLine($"The parent node {Parent} is not inside the tree {tree}.");
            return;
        }
        
        if(!Parent.CanAddChild(Child)) return;
        
        Parent.AddChildRightAway(Child);
        tree.RegisterSubtree(Child);
    }
}