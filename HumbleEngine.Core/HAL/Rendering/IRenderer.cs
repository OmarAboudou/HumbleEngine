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
}
