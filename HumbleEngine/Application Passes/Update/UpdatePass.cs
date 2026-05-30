namespace HumbleEngine;

public class UpdatePass : IUpdatePass
{
    public void Execute(Node root, double delta)
    {
        foreach (Node node in root.GetSubtreeInDepthFirstOrder())
            if (node is IUpdated updated)
                updated.OnUpdate(delta);
    }
}