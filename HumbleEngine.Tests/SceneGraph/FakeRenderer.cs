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

    /// <summary>Quads received by <see cref="DrawQuad"/>, in submission order.</summary>
    public List<(Rect Rect, Vector4 Color)> Quads { get; } = [];

    /// <summary>Textures created by this renderer, in creation order.</summary>
    public List<FakeTexture> Textures { get; } = [];

    /// <summary>Textured quads received by <see cref="DrawTexturedQuad"/>, in submission order.</summary>
    public List<(Rect Rect, ITexture Texture, Rect UvSubRect, Vector4 Tint)> TexturedQuads { get; } = [];

    /// <summary>Glyphs received by <see cref="DrawGlyph"/>, in submission order.</summary>
    public List<(Rect Rect, ITexture Atlas, Rect UvSubRect, Vector4 Color)> Glyphs { get; } = [];

    public bool BeginFrame() => true;

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

    public void DrawQuad(Rect rect, Vector4 color) => Quads.Add((rect, color));

    public ITexture CreateTexture(ReadOnlySpan<byte> pixels, int width, int height, TextureFormat format)
    {
        var texture = new FakeTexture(width, height);
        Textures.Add(texture);
        return texture;
    }

    public void DrawTexturedQuad(Rect rect, ITexture texture, Rect uvSubRect, Vector4 tint) =>
        TexturedQuads.Add((rect, texture, uvSubRect, tint));

    public void DrawGlyph(Rect rect, ITexture atlas, Rect uvSubRect, Vector4 color) =>
        Glyphs.Add((rect, atlas, uvSubRect, color));

    public void Dispose()
    {
    }
}

/// <summary>
/// <see cref="ITexture"/> test double: remembers its size and whether it was
/// disposed — the assertion surface of the texture-ownership story.
/// </summary>
internal sealed class FakeTexture(int width, int height) : ITexture
{
    /// <inheritdoc/>
    public int Width { get; } = width;

    /// <inheritdoc/>
    public int Height { get; } = height;

    /// <summary>True once <see cref="Dispose"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

/// <summary><see cref="IClipboard"/> test double: an in-memory text cell.</summary>
internal sealed class FakeClipboard : IClipboard
{
    public string? Text { get; private set; }

    public void SetText(string text) => Text = text;

    public string? GetText() => Text;
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
