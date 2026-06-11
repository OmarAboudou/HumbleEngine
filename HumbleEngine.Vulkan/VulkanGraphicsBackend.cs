namespace HumbleEngine.Vulkan;

/// <summary>Vulkan graphics backend — stub, not yet implemented.</summary>
public sealed class VulkanGraphicsBackend : IGraphicsBackend
{
    /// <inheritdoc/>
    public string Name => "Vulkan";

    /// <inheritdoc/>
    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    /// <inheritdoc/>
    public void Initialize() =>
        throw new NotImplementedException("Vulkan backend not yet implemented.");

    /// <inheritdoc/>
    public IRenderer CreateRenderer(IGraphicsSurface surface) =>
        throw new NotImplementedException("Vulkan backend not yet implemented.");

    /// <inheritdoc/>
    public void Dispose() { }
}
