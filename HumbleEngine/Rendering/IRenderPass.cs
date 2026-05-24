namespace HumbleEngine;

public interface IRenderPass : IPass
{
    void Execute(Node root, RenderContext context, BlackBoard board);
}
