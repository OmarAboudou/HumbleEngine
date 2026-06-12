namespace HumbleEngine.Sandbox;

/// <summary>
/// The roadmap-06 triangle as a default-constructible node (roadmap 09: every
/// node must be instantiable without arguments — the editor's contract). Its
/// mesh follows the tree lifecycle: created in <see cref="OnAttached"/> from
/// the context renderer, released in <see cref="OnDetached"/> — moving the node
/// to another tree releases and reacquires, and disposal detaches first, so
/// there is a single release path.
/// </summary>
public sealed class TriangleNode : VisualNode
{
    private IMesh? _mesh;

    /// <inheritdoc />
    protected override void OnAttached() =>
        _mesh = Renderer!.CreateMesh(
        [
            new Vertex(new Vector2( 0.0f, -0.5f), new Vector3(1f, 0f, 0f)),
            new Vertex(new Vector2( 0.5f,  0.5f), new Vector3(0f, 1f, 0f)),
            new Vertex(new Vector2(-0.5f,  0.5f), new Vector3(0f, 0f, 1f)),
        ]);

    /// <inheritdoc />
    protected override void OnDetached()
    {
        _mesh?.Dispose();
        _mesh = null;
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer) =>
        renderer.Draw(_mesh!); // drawing implies being in tree, hence attached
}
