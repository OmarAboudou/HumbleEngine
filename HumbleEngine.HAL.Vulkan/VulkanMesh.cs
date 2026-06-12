namespace HumbleEngine.Vulkan;

/// <summary>
/// GPU-resident geometry: a host-visible vertex buffer, its memory, and the
/// vertex count the draw consumes. Disposal waits for the device to go idle —
/// with a single frame in flight the buffer may still feed the frame being
/// rendered. The stall is the learning-grade answer; deferred destruction
/// arrives with frames in flight.
/// </summary>
internal sealed class VulkanMesh(VulkanRenderer owner, IntPtr device, ulong buffer, ulong memory, uint vertexCount) : IMesh
{
    private bool _disposed;

    /// <summary>
    /// The renderer that created this mesh — the buffer lives on its device,
    /// so drawing it anywhere else is rejected up front.
    /// </summary>
    internal VulkanRenderer Owner { get; } = owner;

    /// <summary>The <c>VkBuffer</c> bound at draw time.</summary>
    internal ulong Buffer { get; } = buffer;

    /// <summary>Number of vertices submitted by a draw of this mesh.</summary>
    internal uint VertexCount { get; } = vertexCount;

    /// <summary>
    /// Destroys the buffer then frees its memory (reverse of the bind order),
    /// after a device idle. Must run before the owning renderer's disposal —
    /// the usual reverse creation order. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        VulkanNative.vkDeviceWaitIdle(device);
        VulkanNative.vkDestroyBuffer(device, Buffer, IntPtr.Zero);
        VulkanNative.vkFreeMemory(device, memory, IntPtr.Zero);
    }
}
