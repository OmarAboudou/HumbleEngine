namespace HumbleEngine.Sandbox;

/// <summary>
/// The roadmap-06 triangle, now owned by the tree (roadmap 07, Bloc 3): the
/// mesh is created on the injected renderer at construction, drawn every frame
/// by the tree traversal, and destroyed with the node — remove the node and the
/// triangle is gone.
/// </summary>
public sealed class TriangleNode : VisualNode
{
    private readonly IMesh _mesh;

    /// <summary>Uploads the triangle (NDC positions, one RGB primary per corner).</summary>
    public TriangleNode(IRenderer renderer)
    {
        _mesh = renderer.CreateMesh(
        [
            new Vertex(new Vector2( 0.0f, -0.5f), new Vector3(1f, 0f, 0f)),
            new Vertex(new Vector2( 0.5f,  0.5f), new Vector3(0f, 1f, 0f)),
            new Vertex(new Vector2(-0.5f,  0.5f), new Vector3(0f, 0f, 1f)),
        ]);
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer) => renderer.Draw(_mesh);

    /// <inheritdoc />
    protected override void OnDispose() => _mesh.Dispose();
}
