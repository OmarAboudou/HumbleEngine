using HumbleEngine;

namespace HumbleEngine.Vulkan;

internal sealed class VulkanRenderer : IRenderer
{
    public void BeginFrame() => throw new NotImplementedException("Vulkan renderer not yet implemented.");
    public void EndFrame()   => throw new NotImplementedException("Vulkan renderer not yet implemented.");
    public void Present()    => throw new NotImplementedException("Vulkan renderer not yet implemented.");
    public void Dispose()    { }
}
