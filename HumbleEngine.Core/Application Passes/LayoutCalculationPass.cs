namespace HumbleEngine.Core;

public class LayoutCalculationPass : IUpdatePass
{
    public void Execute(Node root, double delta)
    {
        IEnumerable<UI> uiNodes = root.GetSubtreeInDepthFirstOrder().OfType<UI>();
        foreach (UI uiNode in uiNodes)
        {
            
        }
    }
}