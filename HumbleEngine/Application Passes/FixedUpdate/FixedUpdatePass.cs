namespace HumbleEngine;

public class FixedUpdatePass : IFixedUpdatePass
{
    public void Execute(Node root, double delta)
    {
        foreach (Node node in root.GetSubtreeInDepthFirstOrder())
            if (node is IFixedUpdated fixedUpdate)
                fixedUpdate.OnFixedUpdate(delta);
    }
}