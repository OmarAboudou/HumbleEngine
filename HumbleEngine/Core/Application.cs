namespace HumbleEngine;

public static class Application
{
    public static void Run(Node root, IViewport viewport)
    {
        root.EnterTree();
        viewport.Run();
        root.ExitTree();
    }
}