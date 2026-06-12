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
    /// Records a clear of a colour image. The image must be in
    /// <see cref="VkImageLayout.TransferDstOptimal"/> (or GENERAL) layout.
    /// Declared for a single subresource range.
    /// </summary>
    [DllImport(LibVulkan)]
    internal static extern void vkCmdClearColorImage(
        IntPtr commandBuffer, ulong image, VkImageLayout imageLayout,
        in VkClearColorValue color, uint rangeCount, in VkImageSubresourceRange range);

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
    FenceCreateInfo              = 8,
    SemaphoreCreateInfo          = 9,
    CommandPoolCreateInfo        = 39,
    CommandBufferAllocateInfo    = 40,
    CommandBufferBeginInfo       = 42,
    ImageMemoryBarrier           = 45,
    /// <summary>Extension values are offset by 1000000000 + 1000 × extension number.</summary>
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

/// <summary>Pixel formats (<c>VkFormat</c>). Subset — only the swapchain formats the engine prefers.</summary>
internal enum VkFormat
{
    Undefined    = 0,
    B8G8R8A8Unorm = 44,
    B8G8R8A8Srgb  = 50,
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
    /// <summary>Destination of transfer/clear commands (vkCmdClearColorImage).</summary>
    TransferDst     = 0x2,
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
    Undefined          = 0,
    /// <summary>Optimal as the destination of transfer/clear commands.</summary>
    TransferDstOptimal = 7,
    /// <summary>Required layout for handing the image to the presentation engine.</summary>
    PresentSrcKhr      = 1000001002,
}

/// <summary>Pipeline stages (<c>VkPipelineStageFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkPipelineStageFlags : uint
{
    TopOfPipe    = 0x1,
    Transfer     = 0x1000,
    BottomOfPipe = 0x2000,
}

/// <summary>Memory access types (<c>VkAccessFlagBits</c>). Subset.</summary>
[Flags]
internal enum VkAccessFlags : uint
{
    None          = 0,
    TransferWrite = 0x1000,
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
