using System.Runtime.InteropServices;

namespace HumbleEngine.Vulkan;

/// <summary>
/// Vulkan graphics backend.
/// <see cref="Initialize"/> creates the VkInstance with the window-system surface
/// extensions available on the machine, then selects the best physical device
/// (discrete GPU preferred). Compatible with <see cref="X11WindowBackend"/>
/// and <see cref="WaylandWindowBackend"/>.
/// </summary>
public sealed class VulkanGraphicsBackend : IGraphicsBackend
{
    /// <summary>A physical device with its display name, ranked by <see cref="Score"/>.</summary>
    private readonly record struct DeviceCandidate(IntPtr Device, string Name, int Score);

    private IntPtr            _instance;
    private DeviceCandidate[] _candidates = [];
    private bool              _initialized;

    /// <inheritdoc/>
    public string Name => "Vulkan";

    /// <inheritdoc/>
    public IReadOnlyList<Type> CompatibleWindowBackends =>
        [typeof(X11WindowBackend), typeof(WaylandWindowBackend)];

    /// <summary>
    /// Name of the preferred GPU selected by <see cref="Initialize"/>, or <c>null</c>
    /// before initialisation. <see cref="CreateRenderer"/> may fall back to another
    /// device when this one cannot present to the target surface.
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>The VkInstance handle — exposed to the renderer for surface creation.</summary>
    internal IntPtr Instance => _instance;

    /// <summary>Whether VK_KHR_xlib_surface was enabled — X11 windows are presentable.</summary>
    internal bool HasXlibSurface { get; private set; }

    /// <summary>Whether VK_KHR_wayland_surface was enabled — Wayland windows are presentable.</summary>
    internal bool HasWaylandSurface { get; private set; }

    /// <summary>
    /// Creates the Vulkan instance and selects a physical device. Idempotent.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// VK_KHR_surface is unavailable, no window-system surface extension
    /// (Xlib/Wayland) is available, a Vulkan call fails, or no GPU is found.
    /// </exception>
    /// <exception cref="DllNotFoundException">The Vulkan loader (libvulkan.so.1) is not installed.</exception>
    public void Initialize()
    {
        if (_initialized) return;

        _instance    = CreateInstance();
        _candidates  = RankPhysicalDevices(_instance);
        DeviceName   = _candidates[0].Name;
        _initialized = true;
    }

    /// <summary>
    /// Creates a Vulkan renderer bound to the given window: VkSurfaceKHR from the
    /// native handles, a logical device with one graphics+present queue, and a swapchain.
    /// Devices are tried in preference order until the swapchain succeeds — on hybrid
    /// laptops the discrete GPU may claim present support yet fail to create a
    /// swapchain for the compositor's surface (e.g. NVIDIA on Wayland), in which
    /// case the integrated GPU takes over.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Initialize"/> has not been called, the required surface extension
    /// was not enabled, or no physical device can present to this surface.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The surface does not expose native handles, is not a window, or its
    /// windowing backend is not in <see cref="CompatibleWindowBackends"/>.
    /// </exception>
    public IRenderer CreateRenderer(IGraphicsSurface surface)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "VulkanGraphicsBackend not initialised. Call Initialize() first.");

        if (surface is not INativeWindowHandle handle || surface is not IWindow window)
            throw new ArgumentException(
                "Surface is not a window exposing native handles.", nameof(surface));

        if (!((IGraphicsBackend)this).Supports(window.Backend))
            throw new ArgumentException(
                $"Window backend '{window.Backend.Name}' is not compatible with Vulkan.", nameof(surface));

        ulong vkSurface = CreateVkSurface(window.Backend, handle);

        try
        {
            var failures = new List<string>();

            foreach (var candidate in _candidates)
            {
                IntPtr device = IntPtr.Zero;
                try
                {
                    uint queueFamilyIndex = FindGraphicsPresentQueueFamily(candidate.Device, vkSurface);
                    device = CreateLogicalDevice(candidate.Device, queueFamilyIndex);
                    VulkanNative.vkGetDeviceQueue(device, queueFamilyIndex, 0, out var queue);

                    var (swapchain, images, format, extent) =
                        CreateSwapchain(candidate.Device, device, vkSurface, surface);

                    var renderer = new VulkanRenderer(
                        _instance, candidate.Device, device, queue, queueFamilyIndex,
                        vkSurface, surface, swapchain, images, format, extent, candidate.Name);

                    // The swapchain now drives the window content (e.g. the Wayland
                    // window must stop attaching its own placeholder buffer).
                    handle.NotifyRendererAttached();
                    return renderer;
                }
                catch (InvalidOperationException e)
                {
                    // This device cannot drive this surface — try the next one.
                    if (device != IntPtr.Zero) VulkanNative.vkDestroyDevice(device, IntPtr.Zero);
                    failures.Add($"{candidate.Name}: {e.Message}");
                }
            }

            throw new InvalidOperationException(
                "No physical device can present to this surface. " + string.Join(" | ", failures));
        }
        catch
        {
            VulkanNative.vkDestroySurfaceKHR(_instance, vkSurface, IntPtr.Zero);
            throw;
        }
    }

    /// <summary>Destroys the VkInstance. Idempotent; the backend can be re-initialised afterwards.</summary>
    public void Dispose()
    {
        if (_instance != IntPtr.Zero)
        {
            VulkanNative.vkDestroyInstance(_instance, IntPtr.Zero);
            _instance = IntPtr.Zero;
        }

        _candidates       = [];
        DeviceName        = null;
        HasXlibSurface    = false;
        HasWaylandSurface = false;
        _initialized      = false;
    }

    /// <summary>
    /// Creates the VkInstance with VK_KHR_surface plus every window-system
    /// surface extension (Xlib, Wayland) the loader reports as available.
    /// In Debug builds, the Khronos validation layer is enabled when installed.
    /// </summary>
    private unsafe IntPtr CreateInstance()
    {
        var available = QueryInstanceExtensions();

        var layers = new List<string>();
#if DEBUG
        if (QueryInstanceLayers().Contains(VkLayerNames.KhronosValidation))
            layers.Add(VkLayerNames.KhronosValidation);
#endif

        if (!available.Contains(VkExtensionNames.Surface))
            throw new InvalidOperationException(
                "VK_KHR_surface is not available. The Vulkan driver cannot present to a window system.");

        var extensions = new List<string> { VkExtensionNames.Surface };
        if (available.Contains(VkExtensionNames.XlibSurface))    extensions.Add(VkExtensionNames.XlibSurface);
        if (available.Contains(VkExtensionNames.WaylandSurface)) extensions.Add(VkExtensionNames.WaylandSurface);

        if (extensions.Count == 1)
            throw new InvalidOperationException(
                "Neither VK_KHR_xlib_surface nor VK_KHR_wayland_surface is available. " +
                "Vulkan cannot present to an X11 or Wayland window on this system.");

        HasXlibSurface    = extensions.Contains(VkExtensionNames.XlibSurface);
        HasWaylandSurface = extensions.Contains(VkExtensionNames.WaylandSurface);

        // Vulkan expects const char* / const char* const* — marshal the names
        // to unmanaged ANSI strings for the duration of the call.
        var engineName     = Marshal.StringToHGlobalAnsi("HumbleEngine");
        var extensionNames = new IntPtr[extensions.Count];
        var layerNames     = new IntPtr[layers.Count];

        try
        {
            for (int i = 0; i < extensions.Count; i++)
                extensionNames[i] = Marshal.StringToHGlobalAnsi(extensions[i]);
            for (int i = 0; i < layers.Count; i++)
                layerNames[i] = Marshal.StringToHGlobalAnsi(layers[i]);

            var appInfo = new VkApplicationInfo
            {
                SType              = VkStructureType.ApplicationInfo,
                ApplicationName    = engineName,
                ApplicationVersion = VulkanNative.MakeApiVersion(1, 0, 0),
                EngineName         = engineName,
                EngineVersion      = VulkanNative.MakeApiVersion(1, 0, 0),
                // 1.3: dynamic rendering is core — verified available on every
                // target device (Bloc 0 of roadmap 06).
                ApiVersion         = VulkanNative.MakeApiVersion(1, 3, 0),
            };

            fixed (IntPtr* extensionNamesPtr = extensionNames)
            fixed (IntPtr* layerNamesPtr = layerNames)
            {
                var createInfo = new VkInstanceCreateInfo
                {
                    SType                 = VkStructureType.InstanceCreateInfo,
                    ApplicationInfo       = (IntPtr)(&appInfo),
                    EnabledLayerCount     = (uint)layers.Count,
                    EnabledLayerNames     = layers.Count > 0 ? (IntPtr)layerNamesPtr : IntPtr.Zero,
                    EnabledExtensionCount = (uint)extensions.Count,
                    EnabledExtensionNames = (IntPtr)extensionNamesPtr,
                };

                Check(VulkanNative.vkCreateInstance(in createInfo, IntPtr.Zero, out var instance),
                      "vkCreateInstance");
                return instance;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(engineName);
            foreach (var name in extensionNames) Marshal.FreeHGlobal(name);
            foreach (var name in layerNames) Marshal.FreeHGlobal(name);
        }
    }

    /// <summary>Lists the instance layers installed on this machine.</summary>
    private static HashSet<string> QueryInstanceLayers()
    {
        uint count = 0;
        Check(VulkanNative.vkEnumerateInstanceLayerProperties(ref count, null),
              "vkEnumerateInstanceLayerProperties (count)");

        var properties = new VkLayerProperties[count];
        if (count > 0)
            Check(VulkanNative.vkEnumerateInstanceLayerProperties(ref count, properties),
                  "vkEnumerateInstanceLayerProperties");

        return properties.Select(p => p.GetName()).ToHashSet();
    }

    /// <summary>Lists the instance extensions available on this machine.</summary>
    private static HashSet<string> QueryInstanceExtensions()
    {
        uint count = 0;
        Check(VulkanNative.vkEnumerateInstanceExtensionProperties(null, ref count, null),
              "vkEnumerateInstanceExtensionProperties (count)");

        var properties = new VkExtensionProperties[count];
        if (count > 0)
            Check(VulkanNative.vkEnumerateInstanceExtensionProperties(null, ref count, properties),
                  "vkEnumerateInstanceExtensionProperties");

        return properties.Select(p => p.GetName()).ToHashSet();
    }

    /// <summary>
    /// Ranks the physical devices by preference: discrete GPU first, then integrated,
    /// then anything else (virtual GPU, software rasteriser). Ties keep driver order.
    /// </summary>
    /// <exception cref="InvalidOperationException">No Vulkan-capable device is present.</exception>
    private static DeviceCandidate[] RankPhysicalDevices(IntPtr instance)
    {
        uint count = 0;
        Check(VulkanNative.vkEnumeratePhysicalDevices(instance, ref count, null),
              "vkEnumeratePhysicalDevices (count)");

        if (count == 0)
            throw new InvalidOperationException(
                "No Vulkan-capable GPU found. Is a Vulkan driver (ICD) installed?");

        var devices = new IntPtr[count];
        Check(VulkanNative.vkEnumeratePhysicalDevices(instance, ref count, devices),
              "vkEnumeratePhysicalDevices");

        var candidates = new DeviceCandidate[count];

        for (int i = 0; i < devices.Length; i++)
        {
            VulkanNative.vkGetPhysicalDeviceProperties(devices[i], out var properties);

            int score = properties.DeviceType switch
            {
                VkPhysicalDeviceType.DiscreteGpu   => 2,
                VkPhysicalDeviceType.IntegratedGpu => 1,
                _                                  => 0,
            };

            candidates[i] = new DeviceCandidate(devices[i], properties.GetDeviceName(), score);
        }

        return candidates.OrderByDescending(c => c.Score).ToArray();
    }

    /// <summary>
    /// Creates the VkSurfaceKHR matching the window system, identified by the
    /// backend that created the window: Xlib for <see cref="X11WindowBackend"/>,
    /// Wayland for <see cref="WaylandWindowBackend"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The matching surface extension was not enabled at <see cref="Initialize"/>.</exception>
    /// <exception cref="ArgumentException">The windowing backend is not recognised.</exception>
    private ulong CreateVkSurface(IWindowBackend backend, INativeWindowHandle handle)
    {
        switch (backend)
        {
            case X11WindowBackend:
            {
                if (!HasXlibSurface)
                    throw new InvalidOperationException(
                        "VK_KHR_xlib_surface is not enabled — the driver cannot present to X11 windows.");

                var createInfo = new VkXlibSurfaceCreateInfoKHR
                {
                    SType   = VkStructureType.XlibSurfaceCreateInfoKhr,
                    Display = handle.GetConnectionHandle(),
                    Window  = (ulong)handle.GetNativeHandle().ToInt64(),
                };

                Check(VulkanNative.vkCreateXlibSurfaceKHR(_instance, in createInfo, IntPtr.Zero, out var surface),
                      "vkCreateXlibSurfaceKHR");
                return surface;
            }

            case WaylandWindowBackend:
            {
                if (!HasWaylandSurface)
                    throw new InvalidOperationException(
                        "VK_KHR_wayland_surface is not enabled — the driver cannot present to Wayland windows.");

                var createInfo = new VkWaylandSurfaceCreateInfoKHR
                {
                    SType   = VkStructureType.WaylandSurfaceCreateInfoKhr,
                    Display = handle.GetConnectionHandle(),
                    Surface = handle.GetNativeHandle(),
                };

                Check(VulkanNative.vkCreateWaylandSurfaceKHR(_instance, in createInfo, IntPtr.Zero, out var surface),
                      "vkCreateWaylandSurfaceKHR");
                return surface;
            }

            default:
                throw new ArgumentException(
                    "Windowing backend not recognised — expected X11WindowBackend or WaylandWindowBackend.",
                    nameof(backend));
        }
    }

    /// <summary>
    /// Finds a queue family supporting both graphics commands and presentation
    /// to the given surface. Virtually all desktop GPUs expose such a family;
    /// separate graphics/present families are not supported by this engine.
    /// </summary>
    /// <exception cref="InvalidOperationException">No combined graphics+present family exists.</exception>
    private static uint FindGraphicsPresentQueueFamily(IntPtr physicalDevice, ulong vkSurface)
    {
        uint count = 0;
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref count, null);

        var families = new VkQueueFamilyProperties[count];
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref count, families);

        for (uint i = 0; i < count; i++)
        {
            if ((families[i].QueueFlags & VkQueueFlags.Graphics) == 0) continue;

            Check(VulkanNative.vkGetPhysicalDeviceSurfaceSupportKHR(physicalDevice, i, vkSurface, out uint supported),
                  "vkGetPhysicalDeviceSurfaceSupportKHR");

            if (supported != 0) return i;
        }

        throw new InvalidOperationException(
            "No queue family supports both graphics and present on this surface.");
    }

    /// <summary>
    /// Creates the logical device with a single queue from the given family,
    /// the VK_KHR_swapchain extension, and the dynamic rendering feature
    /// (1.3 core, opt-in) enabled through the <c>pNext</c> chain.
    /// </summary>
    private static unsafe IntPtr CreateLogicalDevice(IntPtr physicalDevice, uint queueFamilyIndex)
    {
        var swapchainExtension = Marshal.StringToHGlobalAnsi(VkExtensionNames.Swapchain);

        try
        {
            float priority = 1.0f;

            var queueCreateInfo = new VkDeviceQueueCreateInfo
            {
                SType            = VkStructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamilyIndex,
                QueueCount       = 1,
                QueuePriorities  = (IntPtr)(&priority),
            };

            var dynamicRendering = new VkPhysicalDeviceDynamicRenderingFeatures
            {
                SType            = VkStructureType.PhysicalDeviceDynamicRenderingFeatures,
                DynamicRendering = 1,
            };

            var createInfo = new VkDeviceCreateInfo
            {
                SType                 = VkStructureType.DeviceCreateInfo,
                Next                  = (IntPtr)(&dynamicRendering),
                QueueCreateInfoCount  = 1,
                QueueCreateInfos      = (IntPtr)(&queueCreateInfo),
                EnabledExtensionCount = 1,
                EnabledExtensionNames = (IntPtr)(&swapchainExtension),
            };

            Check(VulkanNative.vkCreateDevice(physicalDevice, in createInfo, IntPtr.Zero, out var device),
                  "vkCreateDevice");
            return device;
        }
        finally
        {
            Marshal.FreeHGlobal(swapchainExtension);
        }
    }

    /// <summary>
    /// Creates the swapchain: triple-buffered when allowed (min+1 images),
    /// preferred format B8G8R8A8 sRGB, FIFO present mode (vsync, always available),
    /// images usable as colour attachments.
    /// </summary>
    internal static (ulong Swapchain, ulong[] Images, VkFormat Format, VkExtent2D Extent) CreateSwapchain(
        IntPtr physicalDevice, IntPtr device, ulong vkSurface, IGraphicsSurface surface)
    {
        Check(VulkanNative.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, vkSurface, out var capabilities),
              "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");

        var format = PickSurfaceFormat(physicalDevice, vkSurface);
        var extent = PickExtent(in capabilities, surface);

        uint imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        var compositeAlpha = (capabilities.SupportedCompositeAlpha & VkCompositeAlphaFlagsKhr.Opaque) != 0
            ? VkCompositeAlphaFlagsKhr.Opaque
            : VkCompositeAlphaFlagsKhr.Inherit;

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            SType            = VkStructureType.SwapchainCreateInfoKhr,
            Surface          = vkSurface,
            MinImageCount    = imageCount,
            ImageFormat      = format.Format,
            ImageColorSpace  = format.ColorSpace,
            ImageExtent      = extent,
            ImageArrayLayers = 1,
            ImageUsage       = VkImageUsageFlags.ColorAttachment,
            ImageSharingMode = VkSharingMode.Exclusive,
            PreTransform     = capabilities.CurrentTransform,
            CompositeAlpha   = compositeAlpha,
            PresentMode      = VkPresentModeKhr.Fifo,
            Clipped          = 1,
        };

        Check(VulkanNative.vkCreateSwapchainKHR(device, in createInfo, IntPtr.Zero, out var swapchain),
              "vkCreateSwapchainKHR");

        try
        {
            uint count = 0;
            Check(VulkanNative.vkGetSwapchainImagesKHR(device, swapchain, ref count, null),
                  "vkGetSwapchainImagesKHR (count)");

            var images = new ulong[count];
            Check(VulkanNative.vkGetSwapchainImagesKHR(device, swapchain, ref count, images),
                  "vkGetSwapchainImagesKHR");

            return (swapchain, images, format.Format, extent);
        }
        catch
        {
            VulkanNative.vkDestroySwapchainKHR(device, swapchain, IntPtr.Zero);
            throw;
        }
    }

    /// <summary>
    /// Picks the surface format: B8G8R8A8 sRGB with the sRGB-nonlinear colour
    /// space when available, otherwise the first format the surface offers.
    /// </summary>
    private static VkSurfaceFormatKHR PickSurfaceFormat(IntPtr physicalDevice, ulong vkSurface)
    {
        uint count = 0;
        Check(VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, vkSurface, ref count, null),
              "vkGetPhysicalDeviceSurfaceFormatsKHR (count)");

        if (count == 0)
            throw new InvalidOperationException("The surface offers no pixel format.");

        var formats = new VkSurfaceFormatKHR[count];
        Check(VulkanNative.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, vkSurface, ref count, formats),
              "vkGetPhysicalDeviceSurfaceFormatsKHR");

        foreach (var format in formats)
            if (format is { Format: VkFormat.B8G8R8A8Srgb, ColorSpace: VkColorSpaceKhr.SrgbNonlinear })
                return format;

        return formats[0];
    }

    /// <summary>
    /// Resolves the swapchain extent. X11 reports the window size in
    /// <c>currentExtent</c>; Wayland reports 0xFFFFFFFF ("the swapchain decides"),
    /// in which case the window's pixel size is clamped to the supported range.
    /// </summary>
    /// <exception cref="ArgumentException">The extent is undefined and the surface exposes no size.</exception>
    private static VkExtent2D PickExtent(in VkSurfaceCapabilitiesKHR capabilities, IGraphicsSurface surface)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        if (surface is not IWindow window)
            throw new ArgumentException(
                "The window system leaves the swapchain extent undefined and the surface exposes no size.",
                nameof(surface));

        return new VkExtent2D
        {
            Width  = Math.Clamp((uint)window.Width,  capabilities.MinImageExtent.Width,  capabilities.MaxImageExtent.Width),
            Height = Math.Clamp((uint)window.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height),
        };
    }

    /// <summary>
    /// Throws if <paramref name="result"/> is an error.
    /// Negative VkResult values are errors; positive ones (VK_INCOMPLETE…) are
    /// status codes and pass through.
    /// </summary>
    private static void Check(VkResult result, string operation)
    {
        if (result < 0)
            throw new InvalidOperationException($"{operation} failed: {result}.");
    }
}
