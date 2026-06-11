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

    internal OpenGLRenderer(IntPtr display, ulong window, IntPtr context)
    {
        _display = display;
        _window  = window;
        _context = context;
    }

    /// <summary>
    /// Makes the GLX context current and clears the colour and depth buffers.
    /// </summary>
    public void BeginFrame()
    {
        GLXNative.glXMakeCurrent(_display, _window, _context);
        GLXNative.glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GLXNative.glClear(GLMask.ColorBuffer | GLMask.DepthBuffer);
    }

    /// <summary>Flushes pending GL commands.</summary>
    public void EndFrame() => GLXNative.glFlush();

    /// <summary>Swaps the front and back buffers, presenting the rendered frame.</summary>
    public void Present() => GLXNative.glXSwapBuffers(_display, _window);

    /// <summary>Releases the GLX context and destroys it.</summary>
    public void Dispose()
    {
        GLXNative.glXMakeCurrent(_display, 0, IntPtr.Zero);
        GLXNative.glXDestroyContext(_display, _context);
    }
}
