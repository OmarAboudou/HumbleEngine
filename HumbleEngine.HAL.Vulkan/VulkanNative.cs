using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HumbleEngine.Vulkan;

/// <summary>
/// P/Invoke bindings to the Vulkan loader (<c>libvulkan.so.1</c>).
/// Only the entry points needed by the engine are declared, following the C ABI
/// of <c>vulkan_core.h</c>. Dispatchable handles (VkInstance, VkPhysicalDevice,
/// VkDevice, VkQueue…) are opaque pointers, hence <see cref="IntPtr"/>;
/// non-dispatchable handles (VkSurfaceKHR, VkSwapchainKHR, VkImage…) are 64-bit
/// integers, hence <see cref="ulong"/>.
/// The loader exports the WSI extension entry points (VK_KHR_surface,
/// VK_KHR_xlib_surface, VK_KHR_wayland_surface, VK_KHR_swapchain) as direct
/// symbols, so no <c>vkGetInstanceProcAddr</c> indirection is needed for them.
/// </summary>
internal static class VulkanNative
{
    private const string LibVulkan = "libvulkan.so.1";

    /// <summary>
    /// Builds a packed Vulkan version number, equivalent to <c>VK_MAKE_API_VERSION</c>
    /// with variant 0: 10 bits major, 10 bits minor, 12 bits patch.
    /// </summary>
    internal static uint MakeApiVersion(uint major, uint minor, uint patch) =>
        (major << 22) | (minor << 12) | patch;

    /// <summary>
    /// Lists the instance extensions supported by the loader and drivers.
    /// Two-call idiom: pass <c>null</c> to query the count, then a sized array to fill.
    /// Pass <paramref name="layerName"/>=<c>null</c> for extensions provided by
    /// the implementation itself rather than by a layer.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkEnumerateInstanceExtensionProperties(
        string? layerName, ref uint propertyCount, [Out] VkExtensionProperties[]? properties);

    /// <summary>
    /// Lists the layers installed on the system (validation, profiling…).
    /// Two-call idiom, same as <see cref="vkEnumerateInstanceExtensionProperties"/>.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkEnumerateInstanceLayerProperties(
        ref uint propertyCount, [Out] VkLayerProperties[]? properties);

    /// <summary>
    /// Creates a Vulkan instance — the connection between the application and the driver.
    /// <paramref name="allocator"/> is an optional custom host allocator; always
    /// <see cref="IntPtr.Zero"/> here to use the driver's default.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateInstance(
        in VkInstanceCreateInfo createInfo, IntPtr allocator, out IntPtr instance);

    /// <summary>Destroys a Vulkan instance. All child objects must be destroyed first.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyInstance(IntPtr instance, IntPtr allocator);

    /// <summary>
    /// Lists the physical devices (GPUs) accessible through the instance.
    /// Two-call idiom, same as <see cref="vkEnumerateInstanceExtensionProperties"/>.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkEnumeratePhysicalDevices(
        IntPtr instance, ref uint deviceCount, [Out] IntPtr[]? devices);

    /// <summary>Queries the general properties (name, type, versions…) of a physical device.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetPhysicalDeviceProperties(
        IntPtr physicalDevice, out VkPhysicalDeviceProperties properties);

    // --- Queue families ---

    /// <summary>Lists the queue families of a physical device. Two-call idiom.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetPhysicalDeviceQueueFamilyProperties(
        IntPtr physicalDevice, ref uint propertyCount, [Out] VkQueueFamilyProperties[]? properties);

    /// <summary>
    /// Queries whether a queue family can present images to the given surface.
    /// <paramref name="supported"/> is a VkBool32 (0 or 1).
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkGetPhysicalDeviceSurfaceSupportKHR(
        IntPtr physicalDevice, uint queueFamilyIndex, ulong surface, out uint supported);

    // --- WSI surfaces ---

    /// <summary>Creates a VkSurfaceKHR from an X11 window (VK_KHR_xlib_surface).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateXlibSurfaceKHR(
        IntPtr instance, in VkXlibSurfaceCreateInfoKHR createInfo, IntPtr allocator, out ulong surface);

    /// <summary>Creates a VkSurfaceKHR from a Wayland surface (VK_KHR_wayland_surface).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateWaylandSurfaceKHR(
        IntPtr instance, in VkWaylandSurfaceCreateInfoKHR createInfo, IntPtr allocator, out ulong surface);

    /// <summary>Destroys a VkSurfaceKHR. Any swapchain created from it must be destroyed first.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroySurfaceKHR(IntPtr instance, ulong surface, IntPtr allocator);

    /// <summary>Queries the surface capabilities (image counts, extents, transforms…).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkGetPhysicalDeviceSurfaceCapabilitiesKHR(
        IntPtr physicalDevice, ulong surface, out VkSurfaceCapabilitiesKHR capabilities);

    /// <summary>Lists the pixel format / colour space pairs the surface supports. Two-call idiom.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkGetPhysicalDeviceSurfaceFormatsKHR(
        IntPtr physicalDevice, ulong surface, ref uint formatCount, [Out] VkSurfaceFormatKHR[]? formats);

    // --- Logical device ---

    /// <summary>Creates a logical device (VkDevice) with the requested queues and extensions.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateDevice(
        IntPtr physicalDevice, in VkDeviceCreateInfo createInfo, IntPtr allocator, out IntPtr device);

    /// <summary>Destroys a logical device. All child objects must be destroyed first.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyDevice(IntPtr device, IntPtr allocator);

    /// <summary>Retrieves a queue handle from a logical device. Queues are owned by the device, never destroyed.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetDeviceQueue(
        IntPtr device, uint queueFamilyIndex, uint queueIndex, out IntPtr queue);

    /// <summary>Blocks until all queues of the device are idle — used before tearing down resources.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkDeviceWaitIdle(IntPtr device);

    // --- Swapchain ---

    /// <summary>Creates a swapchain bound to a surface (VK_KHR_swapchain, device-level extension).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateSwapchainKHR(
        IntPtr device, in VkSwapchainCreateInfoKHR createInfo, IntPtr allocator, out ulong swapchain);

    /// <summary>Destroys a swapchain and the images it owns.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroySwapchainKHR(IntPtr device, ulong swapchain, IntPtr allocator);

    /// <summary>
    /// Creates a view over an image — the declared interpretation (format, aspect,
    /// mips, layers) through which the pipeline accesses it. Rendering never
    /// touches a raw image, always a view.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateImageView(
        IntPtr device, in VkImageViewCreateInfo createInfo, IntPtr allocator, out ulong view);

    /// <summary>Destroys an image view. The underlying image is not affected.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyImageView(IntPtr device, ulong view, IntPtr allocator);

    // --- Images, samplers, descriptors (textures) ---

    /// <summary>
    /// Creates an image object — a description (type, format, extent, usage, tiling),
    /// without storage: memory is allocated and bound separately, like a buffer.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateImage(
        IntPtr device, in VkImageCreateInfo createInfo, IntPtr allocator, out ulong image);

    /// <summary>Destroys an image. Its memory is freed separately.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyImage(IntPtr device, ulong image, IntPtr allocator);

    /// <summary>Real size/alignment the image needs and which memory types can back it — the FindMemoryType input.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetImageMemoryRequirements(
        IntPtr device, ulong image, out VkMemoryRequirements requirements);

    /// <summary>Marries an image object to a region of allocated memory.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkBindImageMemory(
        IntPtr device, ulong image, ulong memory, ulong memoryOffset);

    /// <summary>Creates a sampler — how a shader filters and wraps when reading an image.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateSampler(
        IntPtr device, in VkSamplerCreateInfo createInfo, IntPtr allocator, out ulong sampler);

    /// <summary>Destroys a sampler.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroySampler(IntPtr device, ulong sampler, IntPtr allocator);

    /// <summary>
    /// Creates a descriptor set layout — the shape of one set: which bindings
    /// (here a single combined image sampler) the shaders reach, at which stages.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateDescriptorSetLayout(
        IntPtr device, in VkDescriptorSetLayoutCreateInfo createInfo, IntPtr allocator, out ulong setLayout);

    /// <summary>Destroys a descriptor set layout.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyDescriptorSetLayout(IntPtr device, ulong setLayout, IntPtr allocator);

    /// <summary>Creates a descriptor pool — the allocator from which descriptor sets are carved.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateDescriptorPool(
        IntPtr device, in VkDescriptorPoolCreateInfo createInfo, IntPtr allocator, out ulong descriptorPool);

    /// <summary>Destroys a descriptor pool and every set allocated from it.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyDescriptorPool(IntPtr device, ulong descriptorPool, IntPtr allocator);

    /// <summary>Allocates descriptor sets from a pool. Declared for a single set (one layout in, one handle out).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkAllocateDescriptorSets(
        IntPtr device, in VkDescriptorSetAllocateInfo allocateInfo, out ulong descriptorSet);

    /// <summary>Frees descriptor sets back to their pool (pool must allow it). Declared for a single set.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkFreeDescriptorSets(
        IntPtr device, ulong descriptorPool, uint descriptorSetCount, in ulong descriptorSet);

    /// <summary>
    /// Writes resources into descriptor sets. Declared for a single write (no
    /// copies) — points a binding at an image+sampler.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkUpdateDescriptorSets(
        IntPtr device, uint writeCount, in VkWriteDescriptorSet writes, uint copyCount, IntPtr copies);

    // --- Texture-related commands recorded into a command buffer ---

    /// <summary>Copies buffer data into an image. Declared for a single region (the whole mip 0).</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdCopyBufferToImage(
        IntPtr commandBuffer, ulong srcBuffer, ulong dstImage, VkImageLayout dstImageLayout,
        uint regionCount, in VkBufferImageCopy region);

    /// <summary>Binds descriptor sets for subsequent draws. Declared for a single set at <c>firstSet</c>, no dynamic offsets.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdBindDescriptorSets(
        IntPtr commandBuffer, VkPipelineBindPoint bindPoint, ulong layout,
        uint firstSet, uint descriptorSetCount, in ulong descriptorSets,
        uint dynamicOffsetCount, IntPtr dynamicOffsets);

    /// <summary>Frees command buffers back to their pool. Declared for a single buffer.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkFreeCommandBuffers(
        IntPtr device, ulong commandPool, uint commandBufferCount, in IntPtr commandBuffer);

    /// <summary>Blocks until every submission to the queue has completed — the simple wait for a one-shot upload.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkQueueWaitIdle(IntPtr queue);

    // --- Pipeline ---

    /// <summary>
    /// Wraps SPIR-V bytecode into a shader module. The module is only a container:
    /// it can be destroyed as soon as the pipelines using it are created.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateShaderModule(
        IntPtr device, in VkShaderModuleCreateInfo createInfo, IntPtr allocator, out ulong shaderModule);

    /// <summary>Destroys a shader module.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyShaderModule(IntPtr device, ulong shaderModule, IntPtr allocator);

    /// <summary>
    /// Creates a pipeline layout — the declaration of every external resource the
    /// shaders can reach (descriptor sets, push constants). Empty for now: the
    /// triangle's shaders are self-contained.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreatePipelineLayout(
        IntPtr device, in VkPipelineLayoutCreateInfo createInfo, IntPtr allocator, out ulong pipelineLayout);

    /// <summary>Destroys a pipeline layout.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyPipelineLayout(IntPtr device, ulong pipelineLayout, IntPtr allocator);

    /// <summary>
    /// Compiles graphics pipelines — the full assembly-line configuration baked
    /// into real GPU state, up front. Declared for a single pipeline, no cache.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateGraphicsPipelines(
        IntPtr device, ulong pipelineCache, uint createInfoCount,
        in VkGraphicsPipelineCreateInfo createInfo, IntPtr allocator, out ulong pipeline);

    /// <summary>Destroys a pipeline.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyPipeline(IntPtr device, ulong pipeline, IntPtr allocator);

    // --- Buffers and memory ---

    /// <summary>Describes the memory landscape of a GPU: heaps (physical pools) and types (allocation flavours).</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetPhysicalDeviceMemoryProperties(
        IntPtr physicalDevice, out VkPhysicalDeviceMemoryProperties properties);

    /// <summary>
    /// Creates a buffer object — a description (size, usage), deliberately
    /// without storage: memory is allocated and bound separately, which is what
    /// makes sub-allocation possible.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateBuffer(
        IntPtr device, in VkBufferCreateInfo createInfo, IntPtr allocator, out ulong buffer);

    /// <summary>Destroys a buffer. Its memory is freed separately.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyBuffer(IntPtr device, ulong buffer, IntPtr allocator);

    /// <summary>
    /// Real size/alignment the buffer needs, and the bitmask of memory types
    /// that can back it — one input of the FindMemoryType loop.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkGetBufferMemoryRequirements(
        IntPtr device, ulong buffer, out VkMemoryRequirements requirements);

    /// <summary>Allocates device memory from one memory type. Drivers cap the live allocation count (~4096) — real engines sub-allocate.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkAllocateMemory(
        IntPtr device, in VkMemoryAllocateInfo allocateInfo, IntPtr allocator, out ulong memory);

    /// <summary>Frees device memory. Everything bound to it must be destroyed first.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkFreeMemory(IntPtr device, ulong memory, IntPtr allocator);

    /// <summary>Marries a buffer object to a region of allocated memory.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkBindBufferMemory(
        IntPtr device, ulong buffer, ulong memory, ulong memoryOffset);

    /// <summary>
    /// Maps HOST_VISIBLE memory into the process address space — the returned
    /// pointer is ordinary CPU-writable memory.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkMapMemory(
        IntPtr device, ulong memory, ulong offset, ulong size, uint flags, out IntPtr data);

    /// <summary>Unmaps previously mapped memory.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkUnmapMemory(IntPtr device, ulong memory);

    /// <summary>Retrieves the VkImage handles owned by the swapchain. Two-call idiom.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkGetSwapchainImagesKHR(
        IntPtr device, ulong swapchain, ref uint imageCount, [Out] ulong[]? images);

    // --- Command pool & command buffers ---

    /// <summary>Creates a command pool — the allocator from which command buffers are carved.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateCommandPool(
        IntPtr device, in VkCommandPoolCreateInfo createInfo, IntPtr allocator, out ulong commandPool);

    /// <summary>Destroys a command pool and frees all command buffers allocated from it.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyCommandPool(IntPtr device, ulong commandPool, IntPtr allocator);

    /// <summary>
    /// Allocates command buffers from a pool. Declared for a single buffer
    /// (<c>commandBufferCount</c> = 1 → one handle out). Command buffers are
    /// dispatchable handles, hence <see cref="IntPtr"/>.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkAllocateCommandBuffers(
        IntPtr device, in VkCommandBufferAllocateInfo allocateInfo, out IntPtr commandBuffer);

    /// <summary>
    /// Starts recording a command buffer. Implicitly resets it when the pool was
    /// created with <see cref="VkCommandPoolCreateFlags.ResetCommandBuffer"/>.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkBeginCommandBuffer(
        IntPtr commandBuffer, in VkCommandBufferBeginInfo beginInfo);

    /// <summary>Ends recording; the buffer becomes executable.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkEndCommandBuffer(IntPtr commandBuffer);

    // --- Commands recorded into a command buffer ---

    /// <summary>
    /// Records a pipeline barrier. Declared for a single image-layout transition
    /// (no memory/buffer barriers, <c>imageMemoryBarrierCount</c> = 1).
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdPipelineBarrier(
        IntPtr commandBuffer,
        VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, uint dependencyFlags,
        uint memoryBarrierCount, IntPtr memoryBarriers,
        uint bufferMemoryBarrierCount, IntPtr bufferMemoryBarriers,
        uint imageMemoryBarrierCount, in VkImageMemoryBarrier imageMemoryBarrier);

    /// <summary>
    /// Opens a dynamic rendering episode (Vulkan 1.3 core): attachments, render
    /// area and load/store behaviour are declared inline — no render pass object.
    /// Layout transitions around the episode are the application's responsibility.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdBeginRendering(IntPtr commandBuffer, in VkRenderingInfo renderingInfo);

    /// <summary>Closes the current dynamic rendering episode.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdEndRendering(IntPtr commandBuffer);

    /// <summary>Binds a pipeline: every draw that follows runs through its configuration.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdBindPipeline(
        IntPtr commandBuffer, VkPipelineBindPoint pipelineBindPoint, ulong pipeline);

    /// <summary>Sets the viewport — declared dynamic in the pipeline so it survives resizes. Single viewport.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdSetViewport(
        IntPtr commandBuffer, uint firstViewport, uint viewportCount, in VkViewport viewport);

    /// <summary>Sets the scissor rectangle — declared dynamic alongside the viewport. Single scissor.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdSetScissor(
        IntPtr commandBuffer, uint firstScissor, uint scissorCount, in VkRect2D scissor);

    /// <summary>
    /// Records a draw: runs the bound pipeline's vertex shader
    /// <paramref name="vertexCount"/> times (gl_VertexIndex = firstVertex…).
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdDraw(
        IntPtr commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);

    /// <summary>
    /// Binds vertex buffers to the pipeline's input bindings. Declared for a
    /// single binding (buffer + start offset).
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdBindVertexBuffers(
        IntPtr commandBuffer, uint firstBinding, uint bindingCount, in ulong buffer, in ulong offset);

    /// <summary>
    /// Writes push constants straight into the command buffer — the cheapest
    /// route to a shader (no allocation, no descriptor, per-draw granularity).
    /// Values persist across draws and pipeline binds; reads are interpreted
    /// through a layout push-compatible with the one used here.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdPushConstants(
        IntPtr commandBuffer, ulong layout, VkShaderStageFlags stageFlags,
        uint offset, uint size, IntPtr values);

    // --- Synchronisation ---

    /// <summary>Creates a semaphore — GPU↔GPU synchronisation (queue waits on queue/present engine).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateSemaphore(
        IntPtr device, in VkSemaphoreCreateInfo createInfo, IntPtr allocator, out ulong semaphore);

    /// <summary>Destroys a semaphore.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroySemaphore(IntPtr device, ulong semaphore, IntPtr allocator);

    /// <summary>Creates a fence — GPU→CPU synchronisation (the host waits for a submit to finish).</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkCreateFence(
        IntPtr device, in VkFenceCreateInfo createInfo, IntPtr allocator, out ulong fence);

    /// <summary>Destroys a fence.</summary>
    [DllImport(LibVulkan)]
    internal static extern void vkDestroyFence(IntPtr device, ulong fence, IntPtr allocator);

    /// <summary>Blocks until the fence is signalled. Declared for a single fence.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkWaitForFences(
        IntPtr device, uint fenceCount, in ulong fence, uint waitAll, ulong timeout);

    /// <summary>Returns a fence to the unsignalled state. Declared for a single fence.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkResetFences(IntPtr device, uint fenceCount, in ulong fence);

    // --- Frame cycle ---

    /// <summary>
    /// Acquires the index of the next presentable swapchain image. The semaphore
    /// is signalled when the image is actually ready to be written.
    /// Returns <see cref="VkResult.ErrorOutOfDateKhr"/> when the swapchain no
    /// longer matches the surface (resize) and must be recreated.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkAcquireNextImageKHR(
        IntPtr device, ulong swapchain, ulong timeout, ulong semaphore, ulong fence, out uint imageIndex);

    /// <summary>Submits work to a queue. Declared for a single submit; the fence is signalled on completion.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkQueueSubmit(
        IntPtr queue, uint submitCount, in VkSubmitInfo submit, ulong fence);

    /// <summary>Queues an image for presentation to the surface.</summary>
    [DllImport(LibVulkan)]
    internal static extern VkResult vkQueuePresentKHR(IntPtr queue, in VkPresentInfoKHR presentInfo);
}

/// <summary>Names of the instance layers used by the engine.</summary>
internal static class VkLayerNames
{
    /// <summary>
    /// The Khronos validation layer: intercepts every call and reports API misuse
    /// in detail on stdout — the safety net of any Vulkan development. Enabled in
    /// Debug builds only, and only when installed on the machine.
    /// </summary>
    public const string KhronosValidation = "VK_LAYER_KHRONOS_validation";
}

/// <summary>Names of the instance and device extensions used by the engine.</summary>
internal static class VkExtensionNames
{
    /// <summary>Base surface support — required to present to any window system.</summary>
    public const string Surface = "VK_KHR_surface";

    /// <summary>Surface creation from an X11 window (Xlib).</summary>
    public const string XlibSurface = "VK_KHR_xlib_surface";

    /// <summary>Surface creation from a Wayland <c>wl_surface</c>.</summary>
    public const string WaylandSurface = "VK_KHR_wayland_surface";

    /// <summary>Swapchain support — device-level extension.</summary>
    public const string Swapchain = "VK_KHR_swapchain";
}

/// <summary>
/// Vulkan operation result. Negative values are errors; positive values
/// (e.g. <see cref="Incomplete"/>) are non-fatal status codes.
/// Subset of <c>VkResult</c> — only the values the engine checks explicitly.
/// </summary>
internal enum VkResult
{
    Success                  = 0,
    /// <summary>A fence/query is not yet signalled, or an acquire with a timeout found no image in time.</summary>
    NotReady                 = 1,
    /// <summary>A wait reached its timeout — here a bounded <c>vkAcquireNextImageKHR</c> for an unpresentable surface.</summary>
    Timeout                  = 2,
    /// <summary>A returned array was too small for the full result — the data is truncated, not invalid.</summary>
    Incomplete               = 5,
    /// <summary>The swapchain still works but no longer matches the surface exactly (e.g. after a resize).</summary>
    SuboptimalKhr            = 1000001003,
    ErrorOutOfHostMemory     = -1,
    ErrorOutOfDeviceMemory   = -2,
    ErrorInitializationFailed = -3,
    ErrorLayerNotPresent     = -6,
    ErrorExtensionNotPresent = -7,
    ErrorIncompatibleDriver  = -9,
    /// <summary>The swapchain is incompatible with the surface and must be recreated.</summary>
    ErrorOutOfDateKhr        = -1000001004,
}

/// <summary>
/// Identifies the concrete type of a Vulkan structure (<c>sType</c> field).
/// Every Vulkan create-info struct starts with this tag so the driver can walk
/// <c>pNext</c> chains of unknown structures. Subset — extended as needed.
/// </summary>
internal enum VkStructureType
{
    ApplicationInfo              = 0,
    InstanceCreateInfo           = 1,
    DeviceQueueCreateInfo        = 2,
    DeviceCreateInfo             = 3,
    SubmitInfo                   = 4,
    MemoryAllocateInfo           = 5,
    FenceCreateInfo              = 8,
    SemaphoreCreateInfo          = 9,
    BufferCreateInfo             = 12,
    ImageCreateInfo              = 14,
    ImageViewCreateInfo          = 15,
    ShaderModuleCreateInfo       = 16,
    PipelineShaderStageCreateInfo         = 18,
    PipelineVertexInputStateCreateInfo    = 19,
    PipelineInputAssemblyStateCreateInfo  = 20,
    PipelineViewportStateCreateInfo       = 22,
    PipelineRasterizationStateCreateInfo  = 23,
    PipelineMultisampleStateCreateInfo    = 24,
    PipelineColorBlendStateCreateInfo     = 26,
    PipelineDynamicStateCreateInfo        = 27,
    GraphicsPipelineCreateInfo            = 28,
    PipelineLayoutCreateInfo              = 30,
    SamplerCreateInfo            = 31,
    DescriptorSetLayoutCreateInfo = 32,
    DescriptorPoolCreateInfo     = 33,
    DescriptorSetAllocateInfo    = 34,
    WriteDescriptorSet           = 35,
    CommandPoolCreateInfo        = 39,
    CommandBufferAllocateInfo    = 40,
    CommandBufferBeginInfo       = 42,
    ImageMemoryBarrier           = 45,
    /// <summary>Extension values are offset by 1000000000 + 1000 × extension number.
    /// Dynamic rendering (extension 45, promoted to 1.3 core) keeps its extension-range values.</summary>
    RenderingInfo                          = 1000044000,
    RenderingAttachmentInfo                = 1000044001,
    PipelineRenderingCreateInfo            = 1000044002,
    PhysicalDeviceDynamicRenderingFeatures = 1000044003,
    SwapchainCreateInfoKhr       = 1000001000,
    PresentInfoKhr               = 1000001001,
    XlibSurfaceCreateInfoKhr     = 1000004000,
    WaylandSurfaceCreateInfoKhr  = 1000006000,
}

/// <summary>Physical device categories reported by <c>vkGetPhysicalDeviceProperties</c>.</summary>
internal enum VkPhysicalDeviceType
{
    Other         = 0,
    /// <summary>GPU sharing memory with the CPU (e.g. Intel iGPU).</summary>
    IntegratedGpu = 1,
    /// <summary>Dedicated GPU with its own memory — usually the best choice.</summary>
    DiscreteGpu   = 2,
    VirtualGpu    = 3,
    /// <summary>Software rasteriser (e.g. lavapipe/llvmpipe).</summary>
    Cpu           = 4,
}

/// <summary>Mirror of <c>VkApplicationInfo</c> — identifies the application to the driver.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkApplicationInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    /// <summary>Pointer to a null-terminated ANSI string (<c>const char*</c>).</summary>
    public IntPtr ApplicationName;
    public uint ApplicationVersion;
    /// <summary>Pointer to a null-terminated ANSI string (<c>const char*</c>).</summary>
    public IntPtr EngineName;
    public uint EngineVersion;
    /// <summary>Highest Vulkan API version the application intends to use.</summary>
    public uint ApiVersion;
}

/// <summary>Mirror of <c>VkInstanceCreateInfo</c> — parameters of <c>vkCreateInstance</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkInstanceCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>Pointer to a <see cref="VkApplicationInfo"/>, or <see cref="IntPtr.Zero"/>.</summary>
    public IntPtr ApplicationInfo;
    public uint EnabledLayerCount;
    /// <summary>Pointer to an array of <c>const char*</c> layer names.</summary>
    public IntPtr EnabledLayerNames;
    public uint EnabledExtensionCount;
    /// <summary>Pointer to an array of <c>const char*</c> extension names.</summary>
    public IntPtr EnabledExtensionNames;
}

/// <summary>Mirror of <c>VkLayerProperties</c> — identity card of one installed layer.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkLayerProperties
{
    /// <summary>Layer name as a fixed null-terminated ANSI buffer (<c>VK_MAX_EXTENSION_NAME_SIZE</c> = 256).</summary>
    public fixed byte LayerName[256];
    public uint SpecVersion;
    public uint ImplementationVersion;

    /// <summary>Description as a fixed null-terminated ANSI buffer (<c>VK_MAX_DESCRIPTION_SIZE</c> = 256).</summary>
    public fixed byte Description[256];

    /// <summary>Decodes <see cref="LayerName"/> into a managed string.</summary>
    public string GetName()
    {
        fixed (byte* name = LayerName)
            return Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
    }
}

/// <summary>Mirror of <c>VkExtensionProperties</c> — one supported extension.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkExtensionProperties
{
    /// <summary>Extension name as a fixed null-terminated ANSI buffer (<c>VK_MAX_EXTENSION_NAME_SIZE</c> = 256).</summary>
    public fixed byte ExtensionName[256];
    public uint SpecVersion;

    /// <summary>Decodes <see cref="ExtensionName"/> into a managed string.</summary>
    public string GetName()
    {
        fixed (byte* name = ExtensionName)
            return Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
    }
}

/// <summary>
/// Mirror of <c>VkPhysicalDeviceProperties</c>.
/// Only the header fields are decoded; <c>limits</c> and <c>sparseProperties</c>
/// are kept as an opaque blob until the engine needs them.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkPhysicalDeviceProperties
{
    public uint ApiVersion;
    public uint DriverVersion;
    public uint VendorId;
    public uint DeviceId;
    public VkPhysicalDeviceType DeviceType;
    /// <summary>Device name as a fixed null-terminated ANSI buffer (<c>VK_MAX_PHYSICAL_DEVICE_NAME_SIZE</c> = 256).</summary>
    public fixed byte DeviceName[256];
    public fixed byte PipelineCacheUuid[16];

    // VkPhysicalDeviceLimits + VkPhysicalDeviceSparseProperties, not decoded yet.
    // C layout on 64-bit: 4 bytes padding (limits starts 8-aligned because of VkDeviceSize)
    // + 504 (limits) + 20 (sparse) + 4 (trailing padding) = 532, for a total struct size of 824.
    private fixed byte _limitsAndSparseProperties[532];

    /// <summary>Decodes <see cref="DeviceName"/> into a managed string.</summary>
    public string GetDeviceName()
    {
        fixed (byte* name = DeviceName)
            return Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
    }
}

/// <summary>Capability flags of a queue family (<c>VkQueueFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkQueueFlags : uint
{
    Graphics = 0x1,
    Compute  = 0x2,
    Transfer = 0x4,
}

/// <summary>Pixel/attribute formats (<c>VkFormat</c>). Subset — swapchain formats and vertex attribute layouts.</summary>
internal enum VkFormat
{
    Undefined    = 0,
    /// <summary>One 8-bit unsigned-normalized channel — the glyph coverage atlas (bloc 3).</summary>
    R8Unorm       = 9,
    /// <summary>Four 8-bit unsigned-normalized channels — the RGBA texture (bloc 2).</summary>
    R8G8B8A8Unorm = 37,
    B8G8R8A8Unorm = 44,
    B8G8R8A8Srgb  = 50,
    /// <summary>Two 32-bit floats — a <c>vec2</c> attribute (Vector2).</summary>
    R32G32Sfloat    = 103,
    /// <summary>Three 32-bit floats — a <c>vec3</c> attribute (Vector3).</summary>
    R32G32B32Sfloat = 106,
}

/// <summary>Colour spaces (<c>VkColorSpaceKHR</c>). Subset.</summary>
internal enum VkColorSpaceKhr
{
    SrgbNonlinear = 0,
}

/// <summary>
/// Presentation strategies (<c>VkPresentModeKHR</c>).
/// <see cref="Fifo"/> (vsync queue) is the only mode the spec guarantees.
/// </summary>
internal enum VkPresentModeKhr
{
    Immediate = 0,
    Mailbox   = 1,
    Fifo      = 2,
}

/// <summary>Resource sharing across queue families (<c>VkSharingMode</c>).</summary>
internal enum VkSharingMode
{
    /// <summary>Owned by one queue family at a time — best performance.</summary>
    Exclusive  = 0,
    Concurrent = 1,
}

/// <summary>Alpha compositing modes with other windows (<c>VkCompositeAlphaFlagBitsKHR</c>).</summary>
[Flags]
internal enum VkCompositeAlphaFlagsKhr : uint
{
    Opaque         = 0x1,
    PreMultiplied  = 0x2,
    PostMultiplied = 0x4,
    Inherit        = 0x8,
}

/// <summary>Allowed usages of the swapchain images (<c>VkImageUsageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkImageUsageFlags : uint
{
    /// <summary>Source of transfer commands.</summary>
    TransferSrc     = 0x1,
    /// <summary>Destination of transfer/clear commands (vkCmdClearColorImage, vkCmdCopyBufferToImage).</summary>
    TransferDst     = 0x2,
    /// <summary>Readable from a shader through a sampler — the texture's reason to exist.</summary>
    Sampled         = 0x4,
    ColorAttachment = 0x10,
}

/// <summary>Mirror of <c>VkExtent2D</c> — a width/height pair in pixels.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkExtent2D
{
    public uint Width;
    public uint Height;
}

/// <summary>Mirror of <c>VkExtent3D</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkExtent3D
{
    public uint Width;
    public uint Height;
    public uint Depth;
}

/// <summary>Mirror of <c>VkQueueFamilyProperties</c> — one family of identical queues.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkQueueFamilyProperties
{
    public VkQueueFlags QueueFlags;
    public uint QueueCount;
    public uint TimestampValidBits;
    public VkExtent3D MinImageTransferGranularity;
}

/// <summary>Mirror of <c>VkXlibSurfaceCreateInfoKHR</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkXlibSurfaceCreateInfoKHR
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>The Xlib <c>Display*</c>.</summary>
    public IntPtr Display;
    /// <summary>The X11 <c>Window</c> XID (<c>unsigned long</c>, 8 bytes on 64-bit).</summary>
    public ulong Window;
}

/// <summary>Mirror of <c>VkWaylandSurfaceCreateInfoKHR</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkWaylandSurfaceCreateInfoKHR
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>The <c>wl_display*</c>.</summary>
    public IntPtr Display;
    /// <summary>The <c>wl_surface*</c>.</summary>
    public IntPtr Surface;
}

/// <summary>Mirror of <c>VkDeviceQueueCreateInfo</c> — requests queues from one family.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDeviceQueueCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint QueueFamilyIndex;
    public uint QueueCount;
    /// <summary>Pointer to <see cref="QueueCount"/> floats in [0,1] — scheduling priorities.</summary>
    public IntPtr QueuePriorities;
}

/// <summary>Mirror of <c>VkDeviceCreateInfo</c> — parameters of <c>vkCreateDevice</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDeviceCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint QueueCreateInfoCount;
    /// <summary>Pointer to an array of <see cref="VkDeviceQueueCreateInfo"/>.</summary>
    public IntPtr QueueCreateInfos;
    /// <summary>Deprecated — device layers no longer exist; kept for ABI compatibility.</summary>
    public uint EnabledLayerCount;
    public IntPtr EnabledLayerNames;
    public uint EnabledExtensionCount;
    /// <summary>Pointer to an array of <c>const char*</c> extension names.</summary>
    public IntPtr EnabledExtensionNames;
    /// <summary>Pointer to a <c>VkPhysicalDeviceFeatures</c>, or <see cref="IntPtr.Zero"/> for none.</summary>
    public IntPtr EnabledFeatures;
}

/// <summary>Mirror of <c>VkSurfaceCapabilitiesKHR</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSurfaceCapabilitiesKHR
{
    public uint MinImageCount;
    /// <summary>0 means no upper limit.</summary>
    public uint MaxImageCount;
    /// <summary>Current surface size, or 0xFFFFFFFF×0xFFFFFFFF when the window system lets the swapchain decide (Wayland).</summary>
    public VkExtent2D CurrentExtent;
    public VkExtent2D MinImageExtent;
    public VkExtent2D MaxImageExtent;
    public uint MaxImageArrayLayers;
    public uint SupportedTransforms;
    public uint CurrentTransform;
    public VkCompositeAlphaFlagsKhr SupportedCompositeAlpha;
    public uint SupportedUsageFlags;
}

/// <summary>Mirror of <c>VkSurfaceFormatKHR</c> — a pixel format / colour space pair.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSurfaceFormatKHR
{
    public VkFormat Format;
    public VkColorSpaceKhr ColorSpace;
}

/// <summary>Mirror of <c>VkSwapchainCreateInfoKHR</c> — parameters of <c>vkCreateSwapchainKHR</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSwapchainCreateInfoKHR
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public ulong Surface;
    public uint MinImageCount;
    public VkFormat ImageFormat;
    public VkColorSpaceKhr ImageColorSpace;
    public VkExtent2D ImageExtent;
    /// <summary>Always 1 except for stereoscopic rendering.</summary>
    public uint ImageArrayLayers;
    public VkImageUsageFlags ImageUsage;
    public VkSharingMode ImageSharingMode;
    public uint QueueFamilyIndexCount;
    public IntPtr QueueFamilyIndices;
    /// <summary>Usually the surface's <c>currentTransform</c>.</summary>
    public uint PreTransform;
    public VkCompositeAlphaFlagsKhr CompositeAlpha;
    public VkPresentModeKhr PresentMode;
    /// <summary>VkBool32 — allow the implementation to discard pixels hidden by other windows.</summary>
    public uint Clipped;
    /// <summary>Previous swapchain when recreating after a resize, or 0.</summary>
    public ulong OldSwapchain;
}

/// <summary>Command pool behaviour flags (<c>VkCommandPoolCreateFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkCommandPoolCreateFlags : uint
{
    /// <summary>Allows command buffers to be reset individually (implicitly on vkBeginCommandBuffer).</summary>
    ResetCommandBuffer = 0x2,
}

/// <summary>Fence creation flags (<c>VkFenceCreateFlagBits</c>).</summary>
[Flags]
internal enum VkFenceCreateFlags : uint
{
    /// <summary>Create the fence already signalled — avoids a deadlock on the very first frame's wait.</summary>
    Signaled = 0x1,
}

/// <summary>Image memory layouts (<c>VkImageLayout</c>). Subset.</summary>
internal enum VkImageLayout
{
    /// <summary>Content undefined — valid source of a transition when the previous content is discarded.</summary>
    Undefined                = 0,
    /// <summary>Optimal for being written by the pipeline as a colour attachment.</summary>
    ColorAttachmentOptimal   = 2,
    /// <summary>Optimal for being read from a shader through a sampler.</summary>
    ShaderReadOnlyOptimal    = 5,
    /// <summary>Optimal as the destination of transfer/clear commands.</summary>
    TransferDstOptimal       = 7,
    /// <summary>Required layout for handing the image to the presentation engine.</summary>
    PresentSrcKhr            = 1000001002,
}

/// <summary>Pipeline stages (<c>VkPipelineStageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkPipelineStageFlags : uint
{
    TopOfPipe             = 0x1,
    /// <summary>The fragment shader stage — where a sampled texture is first read.</summary>
    FragmentShader        = 0x80,
    /// <summary>The stage writing colour attachments — where rendering output lands.</summary>
    ColorAttachmentOutput = 0x400,
    Transfer              = 0x1000,
    BottomOfPipe          = 0x2000,
}

/// <summary>Memory access types (<c>VkAccessFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkAccessFlags : uint
{
    None                 = 0,
    /// <summary>Read by a shader — a sampled texture, once transitioned for reading.</summary>
    ShaderRead           = 0x20,
    ColorAttachmentWrite = 0x100,
    /// <summary>Read by a transfer command.</summary>
    TransferRead         = 0x800,
    TransferWrite        = 0x1000,
}

/// <summary>Image view dimensionality (<c>VkImageViewType</c>). Subset.</summary>
internal enum VkImageViewType
{
    Type2D = 1,
}

/// <summary>Image dimensionality (<c>VkImageType</c>). Subset.</summary>
internal enum VkImageType
{
    Type2D = 1,
}

/// <summary>Memory layout of an image's texels (<c>VkImageTiling</c>).</summary>
internal enum VkImageTiling
{
    /// <summary>Implementation-defined, opaque — what a sampled texture wants. Requires a staging upload.</summary>
    Optimal = 0,
    /// <summary>Row-major, host-addressable — limited, slower for sampling.</summary>
    Linear  = 1,
}

/// <summary>Number of samples per pixel (<c>VkSampleCountFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkSampleCountFlags : uint
{
    /// <summary>One sample — no multisampling.</summary>
    Count1 = 0x1,
}

/// <summary>Kinds of descriptor (<c>VkDescriptorType</c>). Subset.</summary>
internal enum VkDescriptorType
{
    /// <summary>An image and its sampler in one binding — what the übershader samples.</summary>
    CombinedImageSampler = 1,
}

/// <summary>Texel filtering (<c>VkFilter</c>).</summary>
internal enum VkFilter
{
    Nearest = 0,
    Linear  = 1,
}

/// <summary>How a sampler treats coordinates outside [0,1] (<c>VkSamplerAddressMode</c>). Subset.</summary>
internal enum VkSamplerAddressMode
{
    Repeat      = 0,
    /// <summary>Clamp to the edge texel — no bleeding across an atlas's neighbours.</summary>
    ClampToEdge = 2,
}

/// <summary>Mip filtering (<c>VkSamplerMipmapMode</c>).</summary>
internal enum VkSamplerMipmapMode
{
    Nearest = 0,
    Linear  = 1,
}

/// <summary>Descriptor pool behaviour (<c>VkDescriptorPoolCreateFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkDescriptorPoolCreateFlags : uint
{
    /// <summary>Allows individual sets to be freed back to the pool — a texture frees its set at Dispose.</summary>
    FreeDescriptorSet = 0x1,
}

/// <summary>Command buffer recording hints (<c>VkCommandBufferUsageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkCommandBufferUsageFlags : uint
{
    /// <summary>Recorded once, submitted once, then reset/freed — the one-shot texture upload.</summary>
    OneTimeSubmit = 0x1,
}

/// <summary>What happens to an attachment's content when a rendering episode opens (<c>VkAttachmentLoadOp</c>).</summary>
internal enum VkAttachmentLoadOp
{
    /// <summary>Preserve the existing content — forces tile preloading on tiled GPUs.</summary>
    Load     = 0,
    /// <summary>Fill with the clear value — free on tiled GPUs, replaces vkCmdClearColorImage here.</summary>
    Clear    = 1,
    DontCare = 2,
}

/// <summary>What happens to an attachment's content when a rendering episode closes (<c>VkAttachmentStoreOp</c>).</summary>
internal enum VkAttachmentStoreOp
{
    /// <summary>Write the result out — required to present it.</summary>
    Store    = 0,
    DontCare = 1,
}

/// <summary>Which kind of work a pipeline bind targets (<c>VkPipelineBindPoint</c>). Subset.</summary>
internal enum VkPipelineBindPoint
{
    Graphics = 0,
}

/// <summary>Programmable stages (<c>VkShaderStageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkShaderStageFlags : uint
{
    Vertex   = 0x1,
    Fragment = 0x10,
}

/// <summary>How vertices are assembled into primitives (<c>VkPrimitiveTopology</c>). Subset.</summary>
internal enum VkPrimitiveTopology
{
    /// <summary>Every 3 vertices form one independent triangle.</summary>
    TriangleList = 3,
}

/// <summary>How polygons are rasterized (<c>VkPolygonMode</c>). Subset.</summary>
internal enum VkPolygonMode
{
    Fill = 0,
}

/// <summary>Which faces are discarded before rasterization (<c>VkCullModeFlagBits</c>). Subset.</summary>
internal enum VkCullModeFlags : uint
{
    /// <summary>No culling — both windings are drawn (UI quads do not cull either).</summary>
    None = 0,
}

/// <summary>Which winding counts as front-facing (<c>VkFrontFace</c>).</summary>
internal enum VkFrontFace
{
    CounterClockwise = 0,
    Clockwise        = 1,
}

/// <summary>Pipeline settings provided at draw time instead of being baked (<c>VkDynamicState</c>). Subset.</summary>
internal enum VkDynamicState
{
    /// <summary>The pipeline survives window resizes thanks to these two.</summary>
    Viewport = 0,
    Scissor  = 1,
}

/// <summary>Mirror of <c>VkCommandPoolCreateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandPoolCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public VkCommandPoolCreateFlags Flags;
    public uint QueueFamilyIndex;
}

/// <summary>Mirror of <c>VkCommandBufferAllocateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandBufferAllocateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public ulong CommandPool;
    /// <summary>0 = primary (submittable to a queue), 1 = secondary.</summary>
    public uint Level;
    public uint CommandBufferCount;
}

/// <summary>Mirror of <c>VkCommandBufferBeginInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandBufferBeginInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>Only meaningful for secondary command buffers — always <see cref="IntPtr.Zero"/> here.</summary>
    public IntPtr InheritanceInfo;
}

/// <summary>Mirror of <c>VkSemaphoreCreateInfo</c> — no parameters beyond the header.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSemaphoreCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
}

/// <summary>Mirror of <c>VkFenceCreateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkFenceCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public VkFenceCreateFlags Flags;
}

/// <summary>Mirror of <c>VkImageSubresourceRange</c> — which mips/layers of an image are targeted.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageSubresourceRange
{
    /// <summary>Bitmask of <c>VkImageAspectFlagBits</c>; 0x1 = colour.</summary>
    public uint AspectMask;
    public uint BaseMipLevel;
    public uint LevelCount;
    public uint BaseArrayLayer;
    public uint LayerCount;

    /// <summary>Colour aspect (<c>VK_IMAGE_ASPECT_COLOR_BIT</c>).</summary>
    public const uint AspectColor = 0x1;
}

/// <summary>
/// Mirror of <c>VkClearColorValue</c> — a union of float/int/uint RGBA in C;
/// only the float interpretation is used here (16 bytes either way).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkClearColorValue(float r, float g, float b, float a)
{
    public float R = r, G = g, B = b, A = a;
}

/// <summary>Mirror of <c>VkImageMemoryBarrier</c> — an image layout transition with memory dependencies.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageMemoryBarrier
{
    public VkStructureType SType;
    public IntPtr Next;
    public VkAccessFlags SrcAccessMask;
    public VkAccessFlags DstAccessMask;
    public VkImageLayout OldLayout;
    public VkImageLayout NewLayout;
    /// <summary><c>VK_QUEUE_FAMILY_IGNORED</c> (0xFFFFFFFF) when no ownership transfer is needed.</summary>
    public uint SrcQueueFamilyIndex;
    public uint DstQueueFamilyIndex;
    public ulong Image;
    public VkImageSubresourceRange SubresourceRange;

    /// <summary><c>VK_QUEUE_FAMILY_IGNORED</c>.</summary>
    public const uint QueueFamilyIgnored = 0xFFFFFFFF;
}

/// <summary>Mirror of <c>VkSubmitInfo</c> — one batch of work for <c>vkQueueSubmit</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSubmitInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint WaitSemaphoreCount;
    /// <summary>Pointer to an array of VkSemaphore (<c>ulong</c>) to wait on before executing.</summary>
    public IntPtr WaitSemaphores;
    /// <summary>Pointer to an array of <see cref="VkPipelineStageFlags"/> — at which stage each wait applies.</summary>
    public IntPtr WaitDstStageMask;
    public uint CommandBufferCount;
    /// <summary>Pointer to an array of VkCommandBuffer (<c>IntPtr</c>).</summary>
    public IntPtr CommandBuffers;
    public uint SignalSemaphoreCount;
    /// <summary>Pointer to an array of VkSemaphore (<c>ulong</c>) signalled when execution completes.</summary>
    public IntPtr SignalSemaphores;
}

/// <summary>Mirror of <c>VkComponentMapping</c> — per-channel swizzle; all-zero = identity.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkComponentMapping
{
    public uint R, G, B, A;
}

/// <summary>Mirror of <c>VkImageViewCreateInfo</c> — parameters of <c>vkCreateImageView</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageViewCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public ulong Image;
    public VkImageViewType ViewType;
    public VkFormat Format;
    public VkComponentMapping Components;
    public VkImageSubresourceRange SubresourceRange;
}

/// <summary>Mirror of <c>VkOffset2D</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkOffset2D
{
    public int X;
    public int Y;
}

/// <summary>Mirror of <c>VkOffset3D</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkOffset3D
{
    public int X;
    public int Y;
    public int Z;
}

/// <summary>Mirror of <c>VkImageCreateInfo</c> — an image description, storage excluded (bound separately).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public VkImageType ImageType;
    public VkFormat Format;
    public VkExtent3D Extent;
    public uint MipLevels;
    public uint ArrayLayers;
    public VkSampleCountFlags Samples;
    public VkImageTiling Tiling;
    public VkImageUsageFlags Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public IntPtr QueueFamilyIndices;
    /// <summary>The layout the image is created in — <see cref="VkImageLayout.Undefined"/> before any transition.</summary>
    public VkImageLayout InitialLayout;
}

/// <summary>Mirror of <c>VkSamplerCreateInfo</c> — how a shader filters and wraps when reading an image.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSamplerCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public VkFilter MagFilter;
    public VkFilter MinFilter;
    public VkSamplerMipmapMode MipmapMode;
    public VkSamplerAddressMode AddressModeU;
    public VkSamplerAddressMode AddressModeV;
    public VkSamplerAddressMode AddressModeW;
    public float MipLodBias;
    /// <summary>VkBool32 — anisotropic filtering off (no device feature needed).</summary>
    public uint AnisotropyEnable;
    public float MaxAnisotropy;
    /// <summary>VkBool32 — no depth-compare sampler.</summary>
    public uint CompareEnable;
    /// <summary><c>VkCompareOp</c> — ignored when compare is disabled.</summary>
    public uint CompareOp;
    public float MinLod;
    public float MaxLod;
    /// <summary><c>VkBorderColor</c> — only used with a clamp-to-border address mode.</summary>
    public uint BorderColor;
    /// <summary>VkBool32 — sample with normalized [0,1] coordinates (the usual case).</summary>
    public uint UnnormalizedCoordinates;
}

/// <summary>Mirror of <c>VkImageSubresourceLayers</c> — which mip/layers a copy targets.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageSubresourceLayers
{
    public uint AspectMask;
    public uint MipLevel;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

/// <summary>Mirror of <c>VkBufferImageCopy</c> — one buffer→image copy region (the whole mip 0 here).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferImageCopy
{
    public ulong BufferOffset;
    /// <summary>0 = tightly packed (row length = image width).</summary>
    public uint BufferRowLength;
    public uint BufferImageHeight;
    public VkImageSubresourceLayers ImageSubresource;
    public VkOffset3D ImageOffset;
    public VkExtent3D ImageExtent;
}

/// <summary>Mirror of <c>VkDescriptorSetLayoutBinding</c> — one binding's shape inside a set.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetLayoutBinding
{
    public uint Binding;
    public VkDescriptorType DescriptorType;
    public uint DescriptorCount;
    public VkShaderStageFlags StageFlags;
    /// <summary>Pointer to immutable samplers, or <see cref="IntPtr.Zero"/>.</summary>
    public IntPtr ImmutableSamplers;
}

/// <summary>Mirror of <c>VkDescriptorSetLayoutCreateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetLayoutCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint BindingCount;
    /// <summary>Pointer to an array of <see cref="VkDescriptorSetLayoutBinding"/>.</summary>
    public IntPtr Bindings;
}

/// <summary>Mirror of <c>VkDescriptorPoolSize</c> — how many descriptors of one type a pool reserves.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorPoolSize
{
    public VkDescriptorType Type;
    public uint DescriptorCount;
}

/// <summary>Mirror of <c>VkDescriptorPoolCreateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorPoolCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public VkDescriptorPoolCreateFlags Flags;
    public uint MaxSets;
    public uint PoolSizeCount;
    /// <summary>Pointer to an array of <see cref="VkDescriptorPoolSize"/>.</summary>
    public IntPtr PoolSizes;
}

/// <summary>Mirror of <c>VkDescriptorSetAllocateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetAllocateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public ulong DescriptorPool;
    public uint DescriptorSetCount;
    /// <summary>Pointer to an array of VkDescriptorSetLayout (<c>ulong</c>).</summary>
    public IntPtr SetLayouts;
}

/// <summary>Mirror of <c>VkDescriptorImageInfo</c> — the image+sampler a binding is pointed at.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorImageInfo
{
    public ulong Sampler;
    public ulong ImageView;
    public VkImageLayout ImageLayout;
}

/// <summary>Mirror of <c>VkWriteDescriptorSet</c> — points a binding at a resource (one image here).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkWriteDescriptorSet
{
    public VkStructureType SType;
    public IntPtr Next;
    public ulong DstSet;
    public uint DstBinding;
    public uint DstArrayElement;
    public uint DescriptorCount;
    public VkDescriptorType DescriptorType;
    /// <summary>Pointer to a <see cref="VkDescriptorImageInfo"/> (image descriptors).</summary>
    public IntPtr ImageInfo;
    public IntPtr BufferInfo;
    public IntPtr TexelBufferView;
}

/// <summary>Mirror of <c>VkRect2D</c> — an offset/extent pair in pixels.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkRect2D
{
    public VkOffset2D Offset;
    public VkExtent2D Extent;
}

/// <summary>
/// Mirror of <c>VkRenderingAttachmentInfo</c> — one attachment of a dynamic
/// rendering episode: the view to draw on, its expected layout, and the
/// load/store behaviour at the episode's boundaries. The resolve fields concern
/// multisampling — zeroed here.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkRenderingAttachmentInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public ulong ImageView;
    public VkImageLayout ImageLayout;
    /// <summary><c>VkResolveModeFlagBits</c> — 0 = no multisample resolve.</summary>
    public uint ResolveMode;
    public ulong ResolveImageView;
    public VkImageLayout ResolveImageLayout;
    public VkAttachmentLoadOp LoadOp;
    public VkAttachmentStoreOp StoreOp;
    /// <summary><c>VkClearValue</c> union (16 bytes) — only the colour interpretation is used here.</summary>
    public VkClearColorValue ClearValue;
}

/// <summary>Mirror of <c>VkRenderingInfo</c> — parameters of <c>vkCmdBeginRendering</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkRenderingInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public VkRect2D RenderArea;
    public uint LayerCount;
    /// <summary>Multiview bitmask — 0 when multiview is not used.</summary>
    public uint ViewMask;
    public uint ColorAttachmentCount;
    /// <summary>Pointer to an array of <see cref="VkRenderingAttachmentInfo"/>.</summary>
    public IntPtr ColorAttachments;
    /// <summary>Pointer to a <see cref="VkRenderingAttachmentInfo"/>, or zero — no depth here.</summary>
    public IntPtr DepthAttachment;
    public IntPtr StencilAttachment;
}

/// <summary>
/// Mirror of <c>VkPhysicalDeviceDynamicRenderingFeatures</c> — chained into
/// <see cref="VkDeviceCreateInfo.Next"/> to enable dynamic rendering at device creation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPhysicalDeviceDynamicRenderingFeatures
{
    public VkStructureType SType;
    public IntPtr Next;
    /// <summary>VkBool32 — 1 to enable.</summary>
    public uint DynamicRendering;
}

/// <summary>Mirror of <c>VkShaderModuleCreateInfo</c> — wraps SPIR-V bytecode.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkShaderModuleCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>Size of the bytecode in <b>bytes</b> (<c>size_t</c>), although <see cref="Code"/> points to uint words.</summary>
    public nuint CodeSize;
    /// <summary>Pointer to the SPIR-V words (must be 4-byte aligned).</summary>
    public IntPtr Code;
}

/// <summary>Mirror of <c>VkPipelineShaderStageCreateInfo</c> — one programmable stage of a pipeline.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineShaderStageCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public VkShaderStageFlags Stage;
    public ulong Module;
    /// <summary>Pointer to the entry point name as a null-terminated ANSI string (<c>main</c>).</summary>
    public IntPtr Name;
    /// <summary>Specialization constants — zero here.</summary>
    public IntPtr SpecializationInfo;
}

/// <summary>
/// Mirror of <c>VkPipelineVertexInputStateCreateInfo</c> — the vertex data layout.
/// All-zero for the first triangle: the vertex shader feeds itself.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineVertexInputStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint VertexBindingDescriptionCount;
    public IntPtr VertexBindingDescriptions;
    public uint VertexAttributeDescriptionCount;
    public IntPtr VertexAttributeDescriptions;
}

/// <summary>Mirror of <c>VkPipelineInputAssemblyStateCreateInfo</c> — how vertices group into primitives.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineInputAssemblyStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public VkPrimitiveTopology Topology;
    /// <summary>VkBool32 — strip-restart, meaningless for lists.</summary>
    public uint PrimitiveRestartEnable;
}

/// <summary>
/// Mirror of <c>VkPipelineViewportStateCreateInfo</c>. Counts only — the actual
/// viewport/scissor are dynamic, provided at draw time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineViewportStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint ViewportCount;
    public IntPtr Viewports;
    public uint ScissorCount;
    public IntPtr Scissors;
}

/// <summary>Mirror of <c>VkPipelineRasterizationStateCreateInfo</c> — fixed-stage rasterizer settings.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineRasterizationStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>VkBool32.</summary>
    public uint DepthClampEnable;
    /// <summary>VkBool32 — discards everything before rasterization (transform feedback only).</summary>
    public uint RasterizerDiscardEnable;
    public VkPolygonMode PolygonMode;
    public VkCullModeFlags CullMode;
    public VkFrontFace FrontFace;
    /// <summary>VkBool32.</summary>
    public uint DepthBiasEnable;
    public float DepthBiasConstantFactor;
    public float DepthBiasClamp;
    public float DepthBiasSlopeFactor;
    /// <summary>Must be 1.0 unless the wideLines feature is enabled.</summary>
    public float LineWidth;
}

/// <summary>Mirror of <c>VkPipelineMultisampleStateCreateInfo</c> — MSAA settings; 1 sample = off.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineMultisampleStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary><c>VkSampleCountFlagBits</c> — 1 = no multisampling.</summary>
    public uint RasterizationSamples;
    /// <summary>VkBool32.</summary>
    public uint SampleShadingEnable;
    public float MinSampleShading;
    public IntPtr SampleMask;
    /// <summary>VkBool32.</summary>
    public uint AlphaToCoverageEnable;
    /// <summary>VkBool32.</summary>
    public uint AlphaToOneEnable;
}

/// <summary>
/// Mirror of <c>VkPipelineColorBlendAttachmentState</c> — blending for one colour
/// attachment. Off for the opaque mesh pipeline (overwrite); the quad pipeline
/// enables classic alpha blending (src·α + dst·(1−α)) for the UI.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineColorBlendAttachmentState
{
    /// <summary>VkBool32.</summary>
    public uint BlendEnable;
    public VkBlendFactor SrcColorBlendFactor;
    public VkBlendFactor DstColorBlendFactor;
    public VkBlendOp ColorBlendOp;
    public VkBlendFactor SrcAlphaBlendFactor;
    public VkBlendFactor DstAlphaBlendFactor;
    public VkBlendOp AlphaBlendOp;
    /// <summary>Which channels are written (<c>VkColorComponentFlags</c>) — 0xF = RGBA.</summary>
    public uint ColorWriteMask;

    /// <summary>RGBA write mask.</summary>
    public const uint WriteAll = 0xF;
}

/// <summary>Mirror of <c>VkPipelineColorBlendStateCreateInfo</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkPipelineColorBlendStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>VkBool32 — bitwise logic ops instead of blending; off.</summary>
    public uint LogicOpEnable;
    public uint LogicOp;
    public uint AttachmentCount;
    /// <summary>Pointer to an array of <see cref="VkPipelineColorBlendAttachmentState"/>.</summary>
    public IntPtr Attachments;
    public fixed float BlendConstants[4];
}

/// <summary>Mirror of <c>VkPipelineDynamicStateCreateInfo</c> — which settings are provided at draw time.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineDynamicStateCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint DynamicStateCount;
    /// <summary>Pointer to an array of <see cref="VkDynamicState"/>.</summary>
    public IntPtr DynamicStates;
}

/// <summary>Multiplier applied to a blend operand (<c>VkBlendFactor</c>). Subset.</summary>
internal enum VkBlendFactor : uint
{
    Zero             = 0,
    One              = 1,
    SrcAlpha         = 6,
    OneMinusSrcAlpha = 7,
}

/// <summary>How the two blend operands combine (<c>VkBlendOp</c>). Subset.</summary>
internal enum VkBlendOp : uint
{
    Add = 0,
}

/// <summary>Mirror of <c>VkPipelineLayoutCreateInfo</c> — empty for self-contained shaders.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineLayoutCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint SetLayoutCount;
    public IntPtr SetLayouts;
    public uint PushConstantRangeCount;
    public IntPtr PushConstantRanges;
}

/// <summary>
/// Mirror of <c>VkPushConstantRange</c> — one window of the push constant block,
/// visible to the given stages from <see cref="Offset"/> over <see cref="Size"/> bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPushConstantRange
{
    public VkShaderStageFlags StageFlags;
    public uint Offset;
    public uint Size;
}

/// <summary>
/// Mirror of <c>VkPipelineRenderingCreateInfo</c> — chained into
/// <see cref="VkGraphicsPipelineCreateInfo.Next"/>: with dynamic rendering there
/// is no render pass to carry the attachment formats, so the pipeline declares
/// them itself. The residue of the render-pass contract, reduced to a field.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineRenderingCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint ViewMask;
    public uint ColorAttachmentCount;
    /// <summary>Pointer to an array of <see cref="VkFormat"/>.</summary>
    public IntPtr ColorAttachmentFormats;
    public VkFormat DepthAttachmentFormat;
    public VkFormat StencilAttachmentFormat;
}

/// <summary>Mirror of <c>VkGraphicsPipelineCreateInfo</c> — the whole assembly line, declared up front.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkGraphicsPipelineCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    public uint StageCount;
    /// <summary>Pointer to an array of <see cref="VkPipelineShaderStageCreateInfo"/>.</summary>
    public IntPtr Stages;
    public IntPtr VertexInputState;
    public IntPtr InputAssemblyState;
    /// <summary>Tessellation — zero, stage unused.</summary>
    public IntPtr TessellationState;
    public IntPtr ViewportState;
    public IntPtr RasterizationState;
    public IntPtr MultisampleState;
    /// <summary>Depth/stencil — zero, no depth attachment.</summary>
    public IntPtr DepthStencilState;
    public IntPtr ColorBlendState;
    public IntPtr DynamicState;
    public ulong Layout;
    /// <summary>0 with dynamic rendering — formats come from the chained <see cref="VkPipelineRenderingCreateInfo"/>.</summary>
    public ulong RenderPass;
    public uint Subpass;
    public ulong BasePipelineHandle;
    public int BasePipelineIndex;
}

/// <summary>Memory type properties (<c>VkMemoryPropertyFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkMemoryPropertyFlags : uint
{
    /// <summary>Lives in VRAM — fastest for the GPU, not directly writable by the CPU.</summary>
    DeviceLocal  = 0x1,
    /// <summary>Mappable by the CPU (<c>vkMapMemory</c>).</summary>
    HostVisible  = 0x2,
    /// <summary>CPU writes reach the GPU without manual cache flushes.</summary>
    HostCoherent = 0x4,
}

/// <summary>What a buffer may be used for (<c>VkBufferUsageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkBufferUsageFlags : uint
{
    /// <summary>Source of a transfer command — the staging buffer of a texture upload.</summary>
    TransferSrc  = 0x1,
    VertexBuffer = 0x80,
}

/// <summary>Mirror of <c>VkMemoryType</c> — one allocation flavour: its properties and which heap backs it.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryType
{
    public VkMemoryPropertyFlags PropertyFlags;
    public uint HeapIndex;
}

/// <summary>Mirror of <c>VkMemoryHeap</c> — one physical pool and its size in bytes.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryHeap
{
    public ulong Size;
    public uint Flags;
}

/// <summary>Fixed-capacity array of <c>VK_MAX_MEMORY_TYPES</c> (32) memory types.</summary>
[InlineArray(32)]
internal struct VkMemoryTypeArray
{
    private VkMemoryType _element0;
}

/// <summary>Fixed-capacity array of <c>VK_MAX_MEMORY_HEAPS</c> (16) memory heaps.</summary>
[InlineArray(16)]
internal struct VkMemoryHeapArray
{
    private VkMemoryHeap _element0;
}

/// <summary>Mirror of <c>VkPhysicalDeviceMemoryProperties</c> — the GPU's memory map.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPhysicalDeviceMemoryProperties
{
    public uint MemoryTypeCount;
    public VkMemoryTypeArray MemoryTypes;
    public uint MemoryHeapCount;
    public VkMemoryHeapArray MemoryHeaps;
}

/// <summary>Mirror of <c>VkBufferCreateInfo</c> — a buffer description, storage excluded.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferCreateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint Flags;
    /// <summary>Size in bytes.</summary>
    public ulong Size;
    public VkBufferUsageFlags Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public IntPtr QueueFamilyIndices;
}

/// <summary>Mirror of <c>VkMemoryRequirements</c> — what backing a resource demands.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryRequirements
{
    public ulong Size;
    public ulong Alignment;
    /// <summary>Bit i set = memory type i can back this resource.</summary>
    public uint MemoryTypeBits;
}

/// <summary>Mirror of <c>VkMemoryAllocateInfo</c> — parameters of <c>vkAllocateMemory</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryAllocateInfo
{
    public VkStructureType SType;
    public IntPtr Next;
    public ulong AllocationSize;
    public uint MemoryTypeIndex;
}

/// <summary>
/// Mirror of <c>VkVertexInputBindingDescription</c> — one vertex data stream:
/// its slot, the byte distance between consecutive vertices, and whether it
/// advances per vertex (0) or per instance (1).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkVertexInputBindingDescription
{
    public uint Binding;
    public uint Stride;
    public uint InputRate;
}

/// <summary>
/// Mirror of <c>VkVertexInputAttributeDescription</c> — one shader input: which
/// <c>layout(location)</c> it feeds, from which binding, in which format, at
/// which byte offset inside the vertex.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkVertexInputAttributeDescription
{
    public uint Location;
    public uint Binding;
    public VkFormat Format;
    public uint Offset;
}

/// <summary>Mirror of <c>VkViewport</c> — the NDC→pixels mapping, depth range included.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkViewport
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;
}

/// <summary>Mirror of <c>VkPresentInfoKHR</c> — parameters of <c>vkQueuePresentKHR</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPresentInfoKHR
{
    public VkStructureType SType;
    public IntPtr Next;
    public uint WaitSemaphoreCount;
    /// <summary>Pointer to an array of VkSemaphore (<c>ulong</c>) to wait on before presenting.</summary>
    public IntPtr WaitSemaphores;
    public uint SwapchainCount;
    /// <summary>Pointer to an array of VkSwapchainKHR (<c>ulong</c>).</summary>
    public IntPtr Swapchains;
    /// <summary>Pointer to an array of image indices (<c>uint</c>), one per swapchain.</summary>
    public IntPtr ImageIndices;
    /// <summary>Optional per-swapchain results — <see cref="IntPtr.Zero"/> for a single swapchain.</summary>
    public IntPtr Results;
}
