namespace HumbleEngine.OpenGL;

/// <summary>
/// OpenGL renderer backed by a GLX context.
/// Manages the per-frame render cycle: clear → draw → flush → swap.
/// </summary>
internal sealed class OpenGLRenderer : IRenderer
{
    private readonly IntPtr _display;
    private readonly ulong  _window;
    private readonly IntPtr _context;
    private bool _disposed;

    internal OpenGLRenderer(IntPtr display, ulong window, IntPtr context)
    {
        _display = display;
        _window  = window;
        _context = context;
    }

    /// <summary>
    /// Makes the GLX context current and clears the colour and depth buffers.
    /// </summary>
    public bool BeginFrame()
    {
        GLXNative.glXMakeCurrent(_display, _window, _context);
        GLXNative.glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GLXNative.glClear(GLMask.ColorBuffer | GLMask.DepthBuffer);
        return true;
    }

    /// <summary>Flushes pending GL commands.</summary>
    public void EndFrame() => GLXNative.glFlush();

    /// <summary>Swaps the front and back buffers, presenting the rendered frame.</summary>
    public void Present() => GLXNative.glXSwapBuffers(_display, _window);

    /// <summary>
    /// Not supported: the OpenGL backend stayed at the GLX-learning stage
    /// (clear + present) and has no drawing path. Bringing it up to the mesh
    /// contract is a separate project, if ever.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public IMesh CreateMesh(ReadOnlySpan<Vertex> vertices) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <inheritdoc cref="CreateMesh"/>
    public void Draw(IMesh mesh) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <inheritdoc cref="CreateMesh"/>
    public void DrawQuad(Rect rect, Vector4 color) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <inheritdoc cref="CreateMesh"/>
    public ITexture CreateTexture(ReadOnlySpan<byte> pixels, int width, int height, TextureFormat format) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <inheritdoc cref="CreateMesh"/>
    public void DrawTexturedQuad(Rect rect, ITexture texture, Rect uvSubRect, Vector4 tint) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <inheritdoc cref="CreateMesh"/>
    public void DrawGlyph(Rect rect, ITexture atlas, Rect uvSubRect, Vector4 color) =>
        throw new NotSupportedException("The OpenGL backend has no drawing path — use Vulkan.");

    /// <summary>Releases the GLX context and destroys it. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GLXNative.glXMakeCurrent(_display, 0, IntPtr.Zero);
        GLXNative.glXDestroyContext(_display, _context);
    }
}
