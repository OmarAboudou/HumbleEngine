using System.Linq;

namespace HumbleEngine;

public class FixedUpdatePass : IFixedUpdatePass
{
    public void Execute(Node root, double delta, BlackBoard board)
    {
        foreach (var node in root.GetSubtreeDepthFirst().OfType<IUpdate>())
            node.Update(delta);
    }
}
