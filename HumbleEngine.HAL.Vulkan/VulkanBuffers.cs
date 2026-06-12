using System.Runtime.InteropServices;

namespace HumbleEngine.Vulkan;

/// <summary>
/// Buffer creation on the direct route: HOST_VISIBLE | HOST_COHERENT memory,
/// mapped once and written by the CPU — no staging, its client (real assets)
/// has not arrived yet. One allocation per buffer: the learning unit; real
/// engines sub-allocate big blocks (drivers cap live allocations around 4096).
/// </summary>
internal static class VulkanBuffers
{
    /// <summary>
    /// Creates a vertex buffer holding <paramref name="vertices"/>: create the
    /// object, find a host-visible memory type among those the buffer accepts,
    /// allocate, bind, map, copy raw bytes, unmap.
    /// </summary>
    /// <exception cref="InvalidOperationException">No suitable memory type, or a Vulkan call failed.</exception>
    internal static unsafe (ulong Buffer, ulong Memory) CreateVertexBuffer<TVertex>(
        IntPtr physicalDevice, IntPtr device, ReadOnlySpan<TVertex> vertices)
        where TVertex : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(vertices);
        ulong buffer = 0;
        ulong memory = 0;

        try
        {
            var bufferInfo = new VkBufferCreateInfo
            {
                SType       = VkStructureType.BufferCreateInfo,
                Size        = (ulong)bytes.Length,
                Usage       = VkBufferUsageFlags.VertexBuffer,
                SharingMode = VkSharingMode.Exclusive,
            };
            Check(VulkanNative.vkCreateBuffer(device, in bufferInfo, IntPtr.Zero, out buffer),
                  "vkCreateBuffer");

            VulkanNative.vkGetBufferMemoryRequirements(device, buffer, out var requirements);

            var allocateInfo = new VkMemoryAllocateInfo
            {
                SType           = VkStructureType.MemoryAllocateInfo,
                AllocationSize  = requirements.Size,
                MemoryTypeIndex = FindMemoryType(
                    physicalDevice, requirements.MemoryTypeBits,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent),
            };
            Check(VulkanNative.vkAllocateMemory(device, in allocateInfo, IntPtr.Zero, out memory),
                  "vkAllocateMemory");
            Check(VulkanNative.vkBindBufferMemory(device, buffer, memory, 0), "vkBindBufferMemory");

            Check(VulkanNative.vkMapMemory(device, memory, 0, (ulong)bytes.Length, 0, out var mapped),
                  "vkMapMemory");
            bytes.CopyTo(new Span<byte>((void*)mapped, bytes.Length));
            VulkanNative.vkUnmapMemory(device, memory);

            return (buffer, memory);
        }
        catch
        {
            if (buffer != 0)
                VulkanNative.vkDestroyBuffer(device, buffer, IntPtr.Zero);
            if (memory != 0)
                VulkanNative.vkFreeMemory(device, memory, IntPtr.Zero);
            throw;
        }
    }

    /// <summary>
    /// The classic loop: crosses the buffer's acceptable types
    /// (<paramref name="typeBits"/>, bit i = type i) with the properties the CPU
    /// route requires, and returns the first matching type index.
    /// </summary>
    /// <exception cref="InvalidOperationException">No memory type satisfies both constraints.</exception>
    private static uint FindMemoryType(
        IntPtr physicalDevice, uint typeBits, VkMemoryPropertyFlags required)
    {
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(physicalDevice, out var properties);

        for (var i = 0; i < properties.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << i)) == 0)
                continue;
            if ((properties.MemoryTypes[i].PropertyFlags & required) == required)
                return (uint)i;
        }

        throw new InvalidOperationException(
            $"No memory type satisfies both the buffer's requirements and {required}.");
    }

    /// <summary>Throws if <paramref name="result"/> is an error (negative VkResult).</summary>
    private static void Check(VkResult result, string operation)
    {
        if (result < 0)
            throw new InvalidOperationException($"{operation} failed: {result}.");
    }
}
