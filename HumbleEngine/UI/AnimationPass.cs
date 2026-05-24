namespace HumbleEngine;

public sealed class AnimationPass : IUpdatePass
{
    public bool ShouldExecute() => true;

    public void Execute(Node root, double delta, BlackBoard board)
    {
        foreach (var node in root.GetSubtreeDepthFirst().OfType<UINode>())
            node.AdvanceAnimations(delta);
    }
}
