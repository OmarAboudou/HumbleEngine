namespace HumbleEngine.Core;

public record RemoveChildCommand(Node Parent, Node Child) : NodeTreeCommand
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

        if (Child.Tree != tree)
        {
            Console.Error.WriteLine($"The child node {Child} is not inside the tree {tree}.");
            return;
        }

        if (!Parent.Children.Contains(Child))
        {
            Console.Error.WriteLine($"The child node {Child} is a child of the parent node {Parent}.");
            return;
        }

        Child.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.TreeExiting();
        });
        Child.GetSubtreeInPrefixOrder().ForEach(node =>
        {
            node.Tree = null;
        });
        Parent.RemoveChild(Child);

    }
}