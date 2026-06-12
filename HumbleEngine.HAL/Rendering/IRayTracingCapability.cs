namespace HumbleEngine;

/// <summary>
/// Optional ray tracing capability.
/// Detected via the <c>is</c> pattern so renderers that do not support it
/// are not required to implement it.
/// </summary>
public interface IRayTracingCapability
{
    /// <summary>Dispatches a ray tracing pass with the given parameters.</summary>
    void TraceRays(RayTracingDescription description);
}

/// <summary>Parameters for a ray tracing pass.</summary>
/// <param name="Width">Render target width in pixels.</param>
/// <param name="Height">Render target height in pixels.</param>
public record RayTracingDescription(int Width, int Height);
