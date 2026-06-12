namespace HumbleEngine.Vulkan;

/// <summary>
/// Vulkan renderer bound to a window surface.
/// Owns the VkSurfaceKHR, the logical device, its graphics+present queue, the
/// swapchain with its image views, and the per-frame objects (command buffer,
/// semaphores, fence). Single frame in flight:
/// <see cref="BeginFrame"/> waits for the previous frame, acquires a swapchain
/// image and opens a dynamic rendering episode (loadOp = clear);
/// <see cref="EndFrame"/> closes the episode and submits the command buffer;
/// <see cref="Present"/> hands the image to the presentation engine.
/// The swapchain is recreated transparently when the surface changes (resize).
/// </summary>
internal sealed class VulkanRenderer : IRenderer
{
    private readonly IntPtr _instance;
    private readonly IntPtr _physicalDevice;
    private readonly IntPtr _device;
    private readonly ulong  _surface;
    private readonly IGraphicsSurface _surfaceRef;
    private readonly Action<int, int>? _onResize;

    private ulong _swapchain;
    private ulong[] _imageViews = [];

    // Per-frame objects (single frame in flight).
    private readonly ulong  _commandPool;
    private readonly IntPtr _commandBuffer;
    private readonly ulong  _imageAvailable;
    private readonly ulong  _renderFinished;
    private readonly ulong  _inFlightFence;

    private uint _imageIndex;
    private bool _swapchainDirty;
    private bool _disposed;

    /// <summary>The queue used for both command submission and presentation.</summary>
    internal IntPtr GraphicsQueue { get; }

    /// <summary>The images owned by the swapchain, acquired/presented in rotation.</summary>
    internal ulong[] Images { get; private set; }

    /// <summary>Pixel format of the swapchain images.</summary>
    internal VkFormat ImageFormat { get; private set; }

    /// <summary>Size of the swapchain images in pixels.</summary>
    internal VkExtent2D Extent { get; private set; }

    /// <summary>
    /// Name of the GPU actually driving this renderer — may differ from the
    /// backend's preferred device when the fallback kicked in.
    /// </summary>
    internal string DeviceName { get; }

    /// <exception cref="InvalidOperationException">A per-frame Vulkan object could not be created.</exception>
    internal VulkanRenderer(
        IntPtr instance, IntPtr physicalDevice, IntPtr device, IntPtr graphicsQueue, uint queueFamilyIndex,
        ulong surface, IGraphicsSurface surfaceRef,
        ulong swapchain, ulong[] images, VkFormat imageFormat, VkExtent2D extent,
        string deviceName)
    {
        _instance       = instance;
        _physicalDevice = physicalDevice;
        _device         = device;
        _surface        = surface;
        _surfaceRef     = surfaceRef;
        _swapchain      = swapchain;
        GraphicsQueue   = graphicsQueue;
        Images          = images;
        ImageFormat     = imageFormat;
        Extent          = extent;
        DeviceName      = deviceName;

        try
        {
            CreateImageViews();

            var poolInfo = new VkCommandPoolCreateInfo
            {
                SType            = VkStructureType.CommandPoolCreateInfo,
                Flags            = VkCommandPoolCreateFlags.ResetCommandBuffer,
                QueueFamilyIndex = queueFamilyIndex,
            };
            Check(VulkanNative.vkCreateCommandPool(device, in poolInfo, IntPtr.Zero, out _commandPool),
                  "vkCreateCommandPool");

            var allocateInfo = new VkCommandBufferAllocateInfo
            {
                SType              = VkStructureType.CommandBufferAllocateInfo,
                CommandPool        = _commandPool,
                Level              = 0, // primary
                CommandBufferCount = 1,
            };
            Check(VulkanNative.vkAllocateCommandBuffers(device, in allocateInfo, out _commandBuffer),
                  "vkAllocateCommandBuffers");

            var semaphoreInfo = new VkSemaphoreCreateInfo { SType = VkStructureType.SemaphoreCreateInfo };
            Check(VulkanNative.vkCreateSemaphore(device, in semaphoreInfo, IntPtr.Zero, out _imageAvailable),
                  "vkCreateSemaphore (image available)");
            Check(VulkanNative.vkCreateSemaphore(device, in semaphoreInfo, IntPtr.Zero, out _renderFinished),
                  "vkCreateSemaphore (render finished)");

            // Signalled at creation so the first BeginFrame does not wait forever.
            var fenceInfo = new VkFenceCreateInfo
            {
                SType = VkStructureType.FenceCreateInfo,
                Flags = VkFenceCreateFlags.Signaled,
            };
            Check(VulkanNative.vkCreateFence(device, in fenceInfo, IntPtr.Zero, out _inFlightFence),
                  "vkCreateFence");
        }
        catch
        {
            DestroyFrameObjects();
            DestroyImageViews();
            throw;
        }

        // A resized window invalidates the swapchain — rebuild it at the next frame.
        if (surfaceRef is IWindow window)
        {
            _onResize = (_, _) => _swapchainDirty = true;
            window.OnResize += _onResize;
        }
    }

    /// <summary>
    /// Waits for the previous frame to finish, acquires the next swapchain image,
    /// transitions it to the colour-attachment layout and opens the dynamic
    /// rendering episode — the clear to dark grey is its <c>loadOp</c>.
    /// Recreates the swapchain first when it is out of date.
    /// </summary>
    public unsafe void BeginFrame()
    {
        if (_swapchainDirty)
        {
            _swapchainDirty = false;
            RecreateSwapchain();
        }

        Check(VulkanNative.vkWaitForFences(_device, 1, in _inFlightFence, 1, ulong.MaxValue),
              "vkWaitForFences");

        var result = VulkanNative.vkAcquireNextImageKHR(
            _device, _swapchain, ulong.MaxValue, _imageAvailable, 0, out _imageIndex);

        if (result == VkResult.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            result = VulkanNative.vkAcquireNextImageKHR(
                _device, _swapchain, ulong.MaxValue, _imageAvailable, 0, out _imageIndex);
        }
        Check(result, "vkAcquireNextImageKHR");

        // Reset only after a successful acquire, otherwise a throw above would
        // leave the fence unsignalled and deadlock the next BeginFrame.
        Check(VulkanNative.vkResetFences(_device, 1, in _inFlightFence), "vkResetFences");

        var beginInfo = new VkCommandBufferBeginInfo { SType = VkStructureType.CommandBufferBeginInfo };
        Check(VulkanNative.vkBeginCommandBuffer(_commandBuffer, in beginInfo), "vkBeginCommandBuffer");

        TransitionImage(Images[_imageIndex],
            VkImageLayout.Undefined, VkImageLayout.ColorAttachmentOptimal,
            VkAccessFlags.None, VkAccessFlags.ColorAttachmentWrite,
            VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.ColorAttachmentOutput);

        var colorAttachment = new VkRenderingAttachmentInfo
        {
            SType       = VkStructureType.RenderingAttachmentInfo,
            ImageView   = _imageViews[_imageIndex],
            ImageLayout = VkImageLayout.ColorAttachmentOptimal,
            LoadOp      = VkAttachmentLoadOp.Clear,
            StoreOp     = VkAttachmentStoreOp.Store,
            ClearValue  = new VkClearColorValue(0.1f, 0.1f, 0.1f, 1.0f),
        };

        var renderingInfo = new VkRenderingInfo
        {
            SType                = VkStructureType.RenderingInfo,
            RenderArea           = new VkRect2D { Extent = Extent },
            LayerCount           = 1,
            ColorAttachmentCount = 1,
            ColorAttachments     = (IntPtr)(&colorAttachment),
        };

        VulkanNative.vkCmdBeginRendering(_commandBuffer, in renderingInfo);
    }

    /// <summary>
    /// Closes the rendering episode, transitions the image to the presentable
    /// layout, ends the command buffer and submits it: wait <c>imageAvailable</c>
    /// at the colour-output stage, signal <c>renderFinished</c> and the in-flight
    /// fence on completion.
    /// </summary>
    public unsafe void EndFrame()
    {
        VulkanNative.vkCmdEndRendering(_commandBuffer);

        TransitionImage(Images[_imageIndex],
            VkImageLayout.ColorAttachmentOptimal, VkImageLayout.PresentSrcKhr,
            VkAccessFlags.ColorAttachmentWrite, VkAccessFlags.None,
            VkPipelineStageFlags.ColorAttachmentOutput, VkPipelineStageFlags.BottomOfPipe);

        Check(VulkanNative.vkEndCommandBuffer(_commandBuffer), "vkEndCommandBuffer");

        ulong  waitSemaphore   = _imageAvailable;
        ulong  signalSemaphore = _renderFinished;
        IntPtr commandBuffer   = _commandBuffer;
        var    waitStage       = VkPipelineStageFlags.ColorAttachmentOutput;

        var submit = new VkSubmitInfo
        {
            SType                = VkStructureType.SubmitInfo,
            WaitSemaphoreCount   = 1,
            WaitSemaphores       = (IntPtr)(&waitSemaphore),
            WaitDstStageMask     = (IntPtr)(&waitStage),
            CommandBufferCount   = 1,
            CommandBuffers       = (IntPtr)(&commandBuffer),
            SignalSemaphoreCount = 1,
            SignalSemaphores     = (IntPtr)(&signalSemaphore),
        };

        Check(VulkanNative.vkQueueSubmit(GraphicsQueue, 1, in submit, _inFlightFence), "vkQueueSubmit");
    }

    /// <summary>
    /// Presents the rendered image, waiting on <c>renderFinished</c>.
    /// An out-of-date or suboptimal swapchain is rebuilt at the next frame.
    /// </summary>
    public unsafe void Present()
    {
        ulong waitSemaphore = _renderFinished;
        ulong swapchain     = _swapchain;
        uint  imageIndex    = _imageIndex;

        var presentInfo = new VkPresentInfoKHR
        {
            SType              = VkStructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            WaitSemaphores     = (IntPtr)(&waitSemaphore),
            SwapchainCount     = 1,
            Swapchains         = (IntPtr)(&swapchain),
            ImageIndices       = (IntPtr)(&imageIndex),
        };

        var result = VulkanNative.vkQueuePresentKHR(GraphicsQueue, in presentInfo);

        if (result is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr)
        {
            _swapchainDirty = true;
            return;
        }
        Check(result, "vkQueuePresentKHR");
    }

    /// <summary>
    /// Waits for the device to finish all work, then destroys in reverse creation
    /// order: per-frame objects → swapchain → device → surface. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_surfaceRef is IWindow window && _onResize is not null)
            window.OnResize -= _onResize;

        VulkanNative.vkDeviceWaitIdle(_device);
        DestroyFrameObjects();
        DestroyImageViews();
        VulkanNative.vkDestroySwapchainKHR(_device, _swapchain, IntPtr.Zero);
        VulkanNative.vkDestroyDevice(_device, IntPtr.Zero);
        VulkanNative.vkDestroySurfaceKHR(_instance, _surface, IntPtr.Zero);
    }

    /// <summary>Destroys the sync objects and the command pool (which frees its command buffer).</summary>
    private void DestroyFrameObjects()
    {
        if (_inFlightFence  != 0) VulkanNative.vkDestroyFence(_device, _inFlightFence, IntPtr.Zero);
        if (_renderFinished != 0) VulkanNative.vkDestroySemaphore(_device, _renderFinished, IntPtr.Zero);
        if (_imageAvailable != 0) VulkanNative.vkDestroySemaphore(_device, _imageAvailable, IntPtr.Zero);
        if (_commandPool    != 0) VulkanNative.vkDestroyCommandPool(_device, _commandPool, IntPtr.Zero);
    }

    /// <summary>
    /// Rebuilds the swapchain against the current surface state (size, transform).
    /// Destroy-then-create after a device idle — simpler than chaining via
    /// <c>oldSwapchain</c>, at the cost of a stall, acceptable for a resize.
    /// </summary>
    private void RecreateSwapchain()
    {
        VulkanNative.vkDeviceWaitIdle(_device);
        DestroyImageViews();
        VulkanNative.vkDestroySwapchainKHR(_device, _swapchain, IntPtr.Zero);

        (_swapchain, var images, var format, var extent) =
            VulkanGraphicsBackend.CreateSwapchain(_physicalDevice, _device, _surface, _surfaceRef);

        Images      = images;
        ImageFormat = format;
        Extent      = extent;
        CreateImageViews();
    }

    /// <summary>
    /// Creates one view per swapchain image — the pipeline's typed window onto
    /// the raw image: colour aspect, single mip, single layer, swapchain format.
    /// Views live and die with the swapchain.
    /// </summary>
    private void CreateImageViews()
    {
        _imageViews = new ulong[Images.Length];
        for (var i = 0; i < Images.Length; i++)
        {
            var createInfo = new VkImageViewCreateInfo
            {
                SType    = VkStructureType.ImageViewCreateInfo,
                Image    = Images[i],
                ViewType = VkImageViewType.Type2D,
                Format   = ImageFormat,
                SubresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageSubresourceRange.AspectColor,
                    LevelCount = 1,
                    LayerCount = 1,
                },
            };
            Check(VulkanNative.vkCreateImageView(_device, in createInfo, IntPtr.Zero, out _imageViews[i]),
                  "vkCreateImageView");
        }
    }

    /// <summary>Destroys the swapchain image views; the images themselves belong to the swapchain.</summary>
    private void DestroyImageViews()
    {
        foreach (var view in _imageViews)
        {
            if (view != 0)
                VulkanNative.vkDestroyImageView(_device, view, IntPtr.Zero);
        }
        _imageViews = [];
    }

    /// <summary>Records a layout transition of the image's colour aspect into the command buffer.</summary>
    private void TransitionImage(
        ulong image, VkImageLayout from, VkImageLayout to,
        VkAccessFlags srcAccess, VkAccessFlags dstAccess,
        VkPipelineStageFlags srcStage, VkPipelineStageFlags dstStage)
    {
        var barrier = new VkImageMemoryBarrier
        {
            SType               = VkStructureType.ImageMemoryBarrier,
            SrcAccessMask       = srcAccess,
            DstAccessMask       = dstAccess,
            OldLayout           = from,
            NewLayout           = to,
            SrcQueueFamilyIndex = VkImageMemoryBarrier.QueueFamilyIgnored,
            DstQueueFamilyIndex = VkImageMemoryBarrier.QueueFamilyIgnored,
            Image               = image,
            SubresourceRange    = new VkImageSubresourceRange
            {
                AspectMask = VkImageSubresourceRange.AspectColor,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        VulkanNative.vkCmdPipelineBarrier(
            _commandBuffer, srcStage, dstStage, 0,
            0, IntPtr.Zero, 0, IntPtr.Zero, 1, in barrier);
    }

    /// <summary>Throws if <paramref name="result"/> is an error (negative VkResult).</summary>
    private static void Check(VkResult result, string operation)
    {
        if (result < 0)
            throw new InvalidOperationException($"{operation} failed: {result}.");
    }
}
