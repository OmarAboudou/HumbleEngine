namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Concrete <see cref="VisualNode"/> for tests: exposes the protected
/// composition API, records draws (optionally into a shared ordered log) and
/// owns an optional mesh disposed with the node — the Sandbox triangle's shape.
/// </summary>
internal sealed class TestVisualNode(string name, List<string>? log = null) : VisualNode
{
    /// <summary>Optional mesh drawn on every <see cref="OnDraw"/> and disposed with the node.</summary>
    public IMesh? Mesh { get; init; }

    /// <summary>The renderer received by the last <see cref="OnDraw"/> call.</summary>
    public IRenderer? LastRenderer { get; private set; }

    /// <summary>The protected <see cref="VisualNode.Renderer"/> protocol, exposed for assertions.</summary>
    public IRenderer? RendererView => Renderer;

    /// <summary>Number of <see cref="OnDraw"/> calls received.</summary>
    public int DrawCount { get; private set; }

    public void AttachChild(Node child) => Attach(child);

    protected override void OnDraw(IRenderer renderer)
    {
        LastRenderer = renderer;
        DrawCount++;
        log?.Add($"{name}:Draw");
        if (Mesh is not null)
            renderer.Draw(Mesh);
    }

    protected override void OnDispose()
    {
        Mesh?.Dispose();
        log?.Add($"{name}:Disposed");
    }
}
