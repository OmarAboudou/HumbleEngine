namespace HumbleEngine.Core;

public record AddChildCommand(Node Parent, Node Child) : NodeTreeCommand
{
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

        if (Child.Tree != null)
        {
            Console.Error.WriteLine($"The child node {Child} is still inside a tree");
            return;
        }
        
        Parent.AddChildRightAway(Child);
        Child.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Tree = tree;
        });
        Child.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.TreeEntered();
        });
    }
}