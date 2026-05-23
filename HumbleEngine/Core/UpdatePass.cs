using System.Linq;

namespace HumbleEngine;

public class UpdatePass : IPass
{
    public void Execute(Node root, double delta)
    {
        foreach (var node in root.GetSubtreeDepthFirst().OfType<IUpdate>())
            node.Update(delta);
    }
}
