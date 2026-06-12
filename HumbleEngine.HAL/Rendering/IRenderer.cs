namespace HumbleEngine;

/// <summary>Renderer bound to a graphics surface — manages the per-frame render cycle.</summary>
public interface IRenderer : IDisposable
{
    /// <summary>Prepares the backend to render the current frame.</summary>
    void BeginFrame();

    /// <summary>Finalises rendering of the current frame.</summary>
    void EndFrame();

    /// <summary>Presents the rendered frame to the screen (swap buffers / queue present).</summary>
    void Present();

    /// <summary>
    /// Uploads vertices to GPU-visible memory and returns the handle to draw
    /// them with. The expensive, rare half of the drawing contract: create
    /// resources up front, submit draws every frame.
    /// </summary>
    IMesh CreateMesh(ReadOnlySpan<Vertex> vertices);

    /// <summary>
    /// Records a draw of <paramref name="mesh"/> into the current frame. Only
    /// valid between <see cref="BeginFrame"/> and <see cref="EndFrame"/>, and
    /// only with a mesh created by this renderer.
    /// </summary>
    void Draw(IMesh mesh);

    /// <summary>
    /// Records a draw of an axis-aligned quad into the current frame: a
    /// pixel-space rectangle (origin at the window's top-left, Y down) filled
    /// with an RGBA colour, alpha-blended over what is already drawn. The UI
    /// primitive — no resource to create or own: the geometry never changes,
    /// only the draw's parameters do. Only valid between
    /// <see cref="BeginFrame"/> and <see cref="EndFrame"/>.
    /// </summary>
    void DrawQuad(Rect rect, Vector4 color);
}
