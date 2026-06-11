using HumbleEngine;

namespace HumbleEngine.Vulkan;

internal sealed class VulkanRenderer : IRenderer
{
    public void BeginFrame() => throw new NotImplementedException("Renderer Vulkan pas encore implémenté.");
    public void EndFrame()   => throw new NotImplementedException();
    public void Present()    => throw new NotImplementedException();
    public void Dispose()    { }
}
