namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// <see cref="IRenderer"/> test double: hands out <see cref="FakeMesh"/> handles
/// and counts draws. No GPU anywhere — the unit tests verify the traversal and
/// the ownership story, never the backend.
/// </summary>
internal sealed class FakeRenderer : IRenderer
{
    /// <summary>Meshes created by this renderer, in creation order.</summary>
    public List<FakeMesh> Meshes { get; } = [];

    /// <summary>Number of <see cref="Draw"/> calls received.</summary>
    public int DrawCount { get; private set; }

    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
    }

    public void Present()
    {
    }

    public IMesh CreateMesh(ReadOnlySpan<Vertex> vertices)
    {
        var mesh = new FakeMesh(vertices.Length);
        Meshes.Add(mesh);
        return mesh;
    }

    public void Draw(IMesh mesh) => DrawCount++;

    public void Dispose()
    {
    }
}

/// <summary>
/// <see cref="IMesh"/> test double: remembers its vertex count and whether it
/// was disposed — the assertion surface of the mesh-ownership tests.
/// </summary>
internal sealed class FakeMesh(int vertexCount) : IMesh
{
    /// <summary>Number of vertices the mesh was created with.</summary>
    public int VertexCount { get; } = vertexCount;

    /// <summary>True once <see cref="Dispose"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
