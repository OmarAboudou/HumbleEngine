namespace HumbleEngine;

public interface IRenderPass
{
    void Execute(Node root, RenderContext context);
    bool ShouldExecute() => true;
}
