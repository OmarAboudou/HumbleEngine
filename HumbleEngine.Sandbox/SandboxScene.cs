namespace HumbleEngine.Sandbox;

/// <summary>
/// Root scene of the Sandbox: composes the triangle node (roadmap 07, Bloc 3)
/// and narrates its tree lifecycle on the console. The renderer is injected —
/// no ambient context, as everywhere in the engine.
/// </summary>
public sealed class SandboxScene : Scene
{
    private TriangleNode? _triangle;

    /// <summary>Builds the interior: one triangle drawing on <paramref name="renderer"/>.</summary>
    public SandboxScene(IRenderer renderer)
    {
        _triangle = new TriangleNode(renderer) { Name = "Triangle" };
        Attach(_triangle);
    }

    /// <summary>
    /// Queues the triangle's disposal, honoured at the end-of-frame flush — the
    /// Bloc 3 proof that the tree drives the drawing: the node dies, the
    /// triangle vanishes. No-op once the triangle is gone.
    /// </summary>
    public void DisposeTriangle()
    {
        if (_triangle is null)
            return;
        Console.WriteLine($"[SceneTree] {_triangle} queued for disposal — the triangle vanishes at end of frame.");
        _triangle.QueueDispose();
        _triangle = null;
    }

    /// <inheritdoc />
    protected override void OnAttached() =>
        Console.WriteLine($"[SceneTree] {this} attached — the tree is alive.");

    /// <inheritdoc />
    protected override void OnDetached() =>
        Console.WriteLine($"[SceneTree] {this} detached.");

    /// <inheritdoc />
    protected override void OnDispose() =>
        Console.WriteLine($"[SceneTree] {this} disposed.");
}
