namespace HumbleEngine.Vulkan;

/// <summary>
/// GPU-resident image: a device-local <c>VkImage</c> (optimal tiling), its
/// memory, the view the shader reads through, and the descriptor set that binds
/// it (set 0, binding 0 — combined image sampler). The sampler is shared and
/// owned by the renderer, so this only frees its own set, view, image and
/// memory. Disposal waits for the device to go idle — with a single frame in
/// flight the texture may still feed the frame being rendered (the mesh's
/// learning-grade answer; deferred destruction arrives with frames in flight).
/// </summary>
internal sealed class VulkanTexture(
    VulkanRenderer owner, IntPtr device, ulong descriptorPool,
    ulong image, ulong memory, ulong view, ulong descriptorSet,
    int width, int height) : ITexture
{
    private bool _disposed;

    /// <summary>
    /// The renderer that created this texture — its image lives on that device,
    /// so drawing it anywhere else is rejected up front.
    /// </summary>
    internal VulkanRenderer Owner { get; } = owner;

    /// <summary>The descriptor set bound before a textured draw.</summary>
    internal ulong DescriptorSet { get; } = descriptorSet;

    /// <inheritdoc/>
    public int Width { get; } = width;

    /// <inheritdoc/>
    public int Height { get; } = height;

    /// <summary>
    /// Frees the descriptor set back to its pool, then destroys the view, image
    /// and memory (reverse of creation), after a device idle. Must run before the
    /// owning renderer's disposal — the usual reverse creation order. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        VulkanNative.vkDeviceWaitIdle(device);
        ulong set = DescriptorSet;
        if (set != 0)
            VulkanNative.vkFreeDescriptorSets(device, descriptorPool, 1, in set);
        if (view != 0)
            VulkanNative.vkDestroyImageView(device, view, IntPtr.Zero);
        if (image != 0)
            VulkanNative.vkDestroyImage(device, image, IntPtr.Zero);
        if (memory != 0)
            VulkanNative.vkFreeMemory(device, memory, IntPtr.Zero);
    }
}
