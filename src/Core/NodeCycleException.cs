using System.Runtime.CompilerServices;

namespace HumbleEngine.Core;

public class NodeCycleException
(
    Node parent, 
    Node invalidChild
)
:
InvalidOperationException
(
    $"'{invalidChild}' cannot become the child of '{parent}' because '{invalidChild}' is " + 
    (ReferenceEquals(parent, invalidChild) ? $"'{parent}'" : $"an ancestor of '{parent}'") + 
    " and that would create a cycle."
)
{
    public Node parent {get;} = parent;
    public Node invalidChild {get;} = invalidChild;
}