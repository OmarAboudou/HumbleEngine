namespace HumbleEngine;

public interface IRayTracingCapability
{
    void TraceRays(RayTracingDescription description);
}

public record RayTracingDescription(int Width, int Height);
