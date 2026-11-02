namespace HumbleEngine.Core;

public class NodeAlreadyHasAParentException(Node invalidParent, Node child)
:
InvalidOperationException
(
    $"'{child}' is already child of '{child.Parent}'" + 
    $" so it can't be added as a child of '{invalidParent}'." + 
    $"Remove '{child}' from the children of '{child.Parent}'" + 
    $" before adding it as a child of '{invalidParent}'.")
{
    public Node child {get;} = child;
    public Node invalidParent {get;} = invalidParent;
}
