using System.Linq;

namespace HumbleEngine;

public class UpdatePass : IPass
{
    public void Execute(PassContext context)
    {
        foreach (var node in context.Root.GetSubtreeDepthFirst().OfType<IUpdate>())
            node.Update(context.Delta);
    }
}
