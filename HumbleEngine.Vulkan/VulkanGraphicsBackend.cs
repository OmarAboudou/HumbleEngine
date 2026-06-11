namespace HumbleEngine.Vulkan;

public sealed class VulkanGraphicsBackend : IGraphicsBackend
{
    public string Name => "Vulkan";

    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    public void Initialize() =>
        throw new NotImplementedException("Backend Vulkan pas encore implémenté.");

    public IRenderer CreateRenderer(IGraphicsSurface surface) =>
        throw new NotImplementedException("Backend Vulkan pas encore implémenté.");

    public void Dispose() { }
}
