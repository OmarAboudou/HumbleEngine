namespace HumbleEngine.Vulkan;

/// <summary>
/// Vulkan renderer bound to a window surface.
/// Owns the VkSurfaceKHR, the logical device, its graphics+present queue, the
/// swapchain with its image views, and the per-frame objects (command buffer,
/// semaphores, fence). Single frame in flight:
/// <see cref="BeginFrame"/> waits for the previous frame, acquires a swapchain
/// image and opens a dynamic rendering episode (loadOp = clear);
/// <see cref="Draw"/> records mesh draws into the open episode;
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
    private ulong _meshPipeline;
    private ulong _meshPipelineLayout;
    private ulong _quadPipeline;
    private ulong _quadPipelineLayout;
    private ulong _boundPipeline;
    private ulong _boundDescriptorSet;

    // Texture machinery: the übershader's set layout (set 0, binding 0 = combined
    // image sampler), the pool every texture's set is carved from, one shared
    // sampler, and a 1×1 white texture bound by flat draws so a valid set is
    // always present (the single-pipeline übershader keeps its descriptor).
    private readonly ulong _descriptorSetLayout;
    private readonly ulong _descriptorPool;
    private readonly ulong _sampler;
    private readonly VulkanTexture _defaultTexture;

    /// <summary>Descriptor-pool ceiling — growth past it is deferred (its client: hundreds of distinct textures).</summary>
    private const uint MaxTextures = 256;

    // Per-frame objects (single frame in flight).
    private readonly ulong  _commandPool;
    private readonly IntPtr _commandBuffer;
    private readonly ulong  _imageAvailable;
    private readonly ulong  _renderFinished;
    private readonly ulong  _inFlightFence;

    private uint _imageIndex;
    private bool _swapchainDirty;
    private bool _frameOpen;
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
            _descriptorSetLayout = CreateTextureSetLayout(device);
            (_meshPipeline, _meshPipelineLayout) = VulkanPipeline.CreateMeshPipeline(device, imageFormat);
            (_quadPipeline, _quadPipelineLayout) =
                VulkanPipeline.CreateQuadPipeline(device, imageFormat, _descriptorSetLayout);

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

            // The texture machinery needs the command pool and queue (one-shot
            // upload), so it comes after the per-frame objects.
            _descriptorPool = CreateDescriptorPool(device);
            _sampler        = CreateSampler(device);
            _defaultTexture = CreateTextureCore(stackalloc byte[] { 255, 255, 255, 255 }, 1, 1, TextureFormat.Rgba8);
        }
        catch
        {
            DestroyTextureMachinery();
            DestroyFrameObjects();
            DestroyPipeline();
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
    /// rendering episode — the clear to dark grey is its <c>loadOp</c> — ready
    /// for <see cref="Draw"/> calls. Recreates the swapchain first when it is
    /// out of date.
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

        // Pipelines and descriptor sets are bound lazily by Draw/DrawQuad; the
        // command buffer was reset, so nothing is bound yet.
        _boundPipeline      = 0;
        _boundDescriptorSet = 0;

        // Viewport and scissor are dynamic pipeline state: provided each frame,
        // so the pipeline itself survives window resizes.
        var viewport = new VkViewport
        {
            Width    = Extent.Width,
            Height   = Extent.Height,
            MaxDepth = 1.0f,
        };
        VulkanNative.vkCmdSetViewport(_commandBuffer, 0, 1, in viewport);

        var scissor = new VkRect2D { Extent = Extent };
        VulkanNative.vkCmdSetScissor(_commandBuffer, 0, 1, in scissor);

        // Pixels → clip for the whole frame: push constants persist across the
        // command buffer's draws, so the übershader matrix is written once here
        // and every DrawQuad only pushes its own 48 bytes.
        var projection = Matrix4x4.CreateOrthographic(0f, Extent.Width, Extent.Height, 0f, 0f, 1f);
        VulkanNative.vkCmdPushConstants(
            _commandBuffer, _quadPipelineLayout,
            VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
            QuadPush.MatrixOffset, QuadPush.MatrixSize, (IntPtr)(&projection));

        _frameOpen = true;
    }

    /// <summary>
    /// Uploads the vertices into a host-visible buffer and wraps it as a mesh —
    /// the expensive, rare half of the drawing contract.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">Buffer creation failed.</exception>
    public IMesh CreateMesh(ReadOnlySpan<Vertex> vertices)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var (buffer, memory) = VulkanBuffers.CreateVertexBuffer(_physicalDevice, _device, vertices);
        return new VulkanMesh(this, _device, buffer, memory, (uint)vertices.Length);
    }

    /// <summary>
    /// Uploads the pixels into a device-local texture and wraps it as an
    /// <see cref="ITexture"/> — the expensive, rare half of the contract.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The renderer is disposed.</exception>
    /// <exception cref="InvalidOperationException">A Vulkan call failed.</exception>
    public ITexture CreateTexture(ReadOnlySpan<byte> pixels, int width, int height, TextureFormat format)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return CreateTextureCore(pixels, width, height, format);
    }

    /// <summary>
    /// Records a draw of the mesh into the open rendering episode: bind the
    /// mesh pipeline (if not current), bind the vertex buffer, draw.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    /// <exception cref="ArgumentException">The mesh was not created by this renderer.</exception>
    public void Draw(IMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (!_frameOpen)
            throw new InvalidOperationException("Draw is only valid between BeginFrame and EndFrame.");
        if (mesh is not VulkanMesh vulkanMesh)
            throw new ArgumentException($"{mesh.GetType().Name} was not created by a Vulkan renderer.", nameof(mesh));
        if (!ReferenceEquals(vulkanMesh.Owner, this))
            throw new ArgumentException(
                "The mesh was created by another renderer — its buffer lives on that renderer's device.", nameof(mesh));

        BindPipeline(_meshPipeline);
        ulong buffer = vulkanMesh.Buffer;
        ulong offset = 0;
        VulkanNative.vkCmdBindVertexBuffers(_commandBuffer, 0, 1, in buffer, in offset);
        VulkanNative.vkCmdDraw(_commandBuffer, vulkanMesh.VertexCount, 1, 0, 0);
    }

    /// <summary>
    /// Records an übershader quad draw into the open episode: a flat-coloured
    /// rect (mode 0). The default white texture is bound so the always-present
    /// descriptor is valid; the full 0,0,1,1 UV is pushed but never sampled.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    public void DrawQuad(Rect rect, Vector4 color)
    {
        if (!_frameOpen)
            throw new InvalidOperationException("DrawQuad is only valid between BeginFrame and EndFrame.");

        var quad = new QuadParams(
            new Vector4(rect.X, rect.Y, rect.Width, rect.Height),
            new Vector4(0f, 0f, 1f, 1f), color, mode: 0);
        RecordQuad(in quad, _defaultTexture);
    }

    /// <summary>
    /// Records an übershader quad draw sampling <paramref name="texture"/>
    /// (mode 1): the pixel rect filled with the texture's
    /// <paramref name="uvSubRect"/>, multiplied by <paramref name="tint"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    /// <exception cref="ArgumentException">The texture was not created by this renderer.</exception>
    public void DrawTexturedQuad(Rect rect, ITexture texture, Rect uvSubRect, Vector4 tint)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (!_frameOpen)
            throw new InvalidOperationException("DrawTexturedQuad is only valid between BeginFrame and EndFrame.");
        if (texture is not VulkanTexture vulkanTexture)
            throw new ArgumentException($"{texture.GetType().Name} was not created by a Vulkan renderer.", nameof(texture));
        if (!ReferenceEquals(vulkanTexture.Owner, this))
            throw new ArgumentException(
                "The texture was created by another renderer — its image lives on that renderer's device.", nameof(texture));

        var quad = new QuadParams(
            new Vector4(rect.X, rect.Y, rect.Width, rect.Height),
            new Vector4(uvSubRect.X, uvSubRect.Y, uvSubRect.Width, uvSubRect.Height),
            tint, mode: 1);
        RecordQuad(in quad, vulkanTexture);
    }

    /// <summary>
    /// Records a glyph draw (mode 2): the coverage atlas sampled in
    /// <paramref name="uvSubRect"/> is used as alpha over the solid
    /// <paramref name="color"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No frame is open.</exception>
    /// <exception cref="ArgumentException">The atlas was not created by this renderer.</exception>
    public void DrawGlyph(Rect rect, ITexture atlas, Rect uvSubRect, Vector4 color)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        if (!_frameOpen)
            throw new InvalidOperationException("DrawGlyph is only valid between BeginFrame and EndFrame.");
        if (atlas is not VulkanTexture vulkanAtlas)
            throw new ArgumentException($"{atlas.GetType().Name} was not created by a Vulkan renderer.", nameof(atlas));
        if (!ReferenceEquals(vulkanAtlas.Owner, this))
            throw new ArgumentException(
                "The atlas was created by another renderer — its image lives on that renderer's device.", nameof(atlas));

        var quad = new QuadParams(
            new Vector4(rect.X, rect.Y, rect.Width, rect.Height),
            new Vector4(uvSubRect.X, uvSubRect.Y, uvSubRect.Width, uvSubRect.Height),
            color, mode: 2);
        RecordQuad(in quad, vulkanAtlas);
    }

    /// <summary>
    /// The shared tail of every quad draw: bind the quad pipeline and the
    /// texture's descriptor set (both lazily), push the per-draw parameters,
    /// draw the six generated vertices.
    /// </summary>
    private unsafe void RecordQuad(in QuadParams quad, VulkanTexture texture)
    {
        BindPipeline(_quadPipeline);
        BindDescriptorSet(texture.DescriptorSet);
        fixed (QuadParams* p = &quad)
            VulkanNative.vkCmdPushConstants(
                _commandBuffer, _quadPipelineLayout,
                VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
                QuadPush.ParamsOffset, QuadPush.ParamsSize, (IntPtr)p);
        VulkanNative.vkCmdDraw(_commandBuffer, 6, 1, 0, 0);
    }

    /// <summary>Binds the pipeline unless it is already the one bound in this command buffer.</summary>
    private void BindPipeline(ulong pipeline)
    {
        if (_boundPipeline == pipeline)
            return;
        VulkanNative.vkCmdBindPipeline(_commandBuffer, VkPipelineBindPoint.Graphics, pipeline);
        _boundPipeline = pipeline;
    }

    /// <summary>Binds the quad pipeline's set 0 unless it is already the one bound.</summary>
    private void BindDescriptorSet(ulong set)
    {
        if (_boundDescriptorSet == set)
            return;
        VulkanNative.vkCmdBindDescriptorSets(
            _commandBuffer, VkPipelineBindPoint.Graphics, _quadPipelineLayout,
            0, 1, in set, 0, IntPtr.Zero);
        _boundDescriptorSet = set;
    }

    /// <summary>
    /// Closes the rendering episode, transitions the image to the presentable
    /// layout, ends the command buffer and submits it: wait <c>imageAvailable</c>
    /// at the colour-output stage, signal <c>renderFinished</c> and the in-flight
    /// fence on completion.
    /// </summary>
    public unsafe void EndFrame()
    {
        _frameOpen = false;
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
        DestroyTextureMachinery();
        DestroyFrameObjects();
        DestroyPipeline();
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

    /// <summary>Destroys both pipelines (mesh, quad) and their layouts.</summary>
    private void DestroyPipeline()
    {
        if (_meshPipeline != 0)
            VulkanNative.vkDestroyPipeline(_device, _meshPipeline, IntPtr.Zero);
        if (_meshPipelineLayout != 0)
            VulkanNative.vkDestroyPipelineLayout(_device, _meshPipelineLayout, IntPtr.Zero);
        if (_quadPipeline != 0)
            VulkanNative.vkDestroyPipeline(_device, _quadPipeline, IntPtr.Zero);
        if (_quadPipelineLayout != 0)
            VulkanNative.vkDestroyPipelineLayout(_device, _quadPipelineLayout, IntPtr.Zero);
        _meshPipeline = 0;
        _meshPipelineLayout = 0;
        _quadPipeline = 0;
        _quadPipelineLayout = 0;
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

    /// <summary>Records a colour-aspect layout transition into the per-frame command buffer.</summary>
    private void TransitionImage(
        ulong image, VkImageLayout from, VkImageLayout to,
        VkAccessFlags srcAccess, VkAccessFlags dstAccess,
        VkPipelineStageFlags srcStage, VkPipelineStageFlags dstStage) =>
        RecordImageTransition(_commandBuffer, image, from, to, srcAccess, dstAccess, srcStage, dstStage);

    /// <summary>Records a colour-aspect layout transition into <paramref name="commandBuffer"/> — frame or one-shot.</summary>
    private static void RecordImageTransition(
        IntPtr commandBuffer, ulong image, VkImageLayout from, VkImageLayout to,
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
            commandBuffer, srcStage, dstStage, 0,
            0, IntPtr.Zero, 0, IntPtr.Zero, 1, in barrier);
    }

    /// <summary>Creates the übershader's descriptor set layout: set 0, binding 0 = combined image sampler, fragment stage.</summary>
    private static unsafe ulong CreateTextureSetLayout(IntPtr device)
    {
        var binding = new VkDescriptorSetLayoutBinding
        {
            Binding         = 0,
            DescriptorType  = VkDescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            StageFlags      = VkShaderStageFlags.Fragment,
        };
        var info = new VkDescriptorSetLayoutCreateInfo
        {
            SType        = VkStructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            Bindings     = (IntPtr)(&binding),
        };
        Check(VulkanNative.vkCreateDescriptorSetLayout(device, in info, IntPtr.Zero, out var layout),
              "vkCreateDescriptorSetLayout");
        return layout;
    }

    /// <summary>Creates the pool every texture's set is carved from — individually freeable, capped at <see cref="MaxTextures"/>.</summary>
    private static unsafe ulong CreateDescriptorPool(IntPtr device)
    {
        var size = new VkDescriptorPoolSize
        {
            Type            = VkDescriptorType.CombinedImageSampler,
            DescriptorCount = MaxTextures,
        };
        var info = new VkDescriptorPoolCreateInfo
        {
            SType         = VkStructureType.DescriptorPoolCreateInfo,
            Flags         = VkDescriptorPoolCreateFlags.FreeDescriptorSet,
            MaxSets       = MaxTextures,
            PoolSizeCount = 1,
            PoolSizes     = (IntPtr)(&size),
        };
        Check(VulkanNative.vkCreateDescriptorPool(device, in info, IntPtr.Zero, out var pool),
              "vkCreateDescriptorPool");
        return pool;
    }

    /// <summary>Creates the shared sampler — linear filtering, clamp to edge (no bleeding across atlas neighbours).</summary>
    private static unsafe ulong CreateSampler(IntPtr device)
    {
        var info = new VkSamplerCreateInfo
        {
            SType        = VkStructureType.SamplerCreateInfo,
            MagFilter    = VkFilter.Linear,
            MinFilter    = VkFilter.Linear,
            MipmapMode   = VkSamplerMipmapMode.Nearest,
            AddressModeU = VkSamplerAddressMode.ClampToEdge,
            AddressModeV = VkSamplerAddressMode.ClampToEdge,
            AddressModeW = VkSamplerAddressMode.ClampToEdge,
        };
        Check(VulkanNative.vkCreateSampler(device, in info, IntPtr.Zero, out var sampler),
              "vkCreateSampler");
        return sampler;
    }

    /// <summary>
    /// The texture-creation body: a device-local optimal-tiling image, the pixels
    /// uploaded through a staging buffer (one-shot copy with the Undefined →
    /// TransferDst → ShaderReadOnly transitions), a view, and a descriptor set
    /// pointing at (view, shared sampler). The staging buffer dies with the copy.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan call failed.</exception>
    private unsafe VulkanTexture CreateTextureCore(
        ReadOnlySpan<byte> pixels, int width, int height, TextureFormat format)
    {
        var vkFormat = format switch
        {
            TextureFormat.Rgba8 => VkFormat.R8G8B8A8Unorm,
            TextureFormat.R8    => VkFormat.R8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown texture format."),
        };

        ulong image = 0, memory = 0, view = 0, set = 0;
        ulong staging = 0, stagingMemory = 0;
        try
        {
            var imageInfo = new VkImageCreateInfo
            {
                SType         = VkStructureType.ImageCreateInfo,
                ImageType     = VkImageType.Type2D,
                Format        = vkFormat,
                Extent        = new VkExtent3D { Width = (uint)width, Height = (uint)height, Depth = 1 },
                MipLevels     = 1,
                ArrayLayers   = 1,
                Samples       = VkSampleCountFlags.Count1,
                Tiling        = VkImageTiling.Optimal,
                Usage         = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
                SharingMode   = VkSharingMode.Exclusive,
                InitialLayout = VkImageLayout.Undefined,
            };
            Check(VulkanNative.vkCreateImage(_device, in imageInfo, IntPtr.Zero, out image), "vkCreateImage");

            VulkanNative.vkGetImageMemoryRequirements(_device, image, out var requirements);
            var allocateInfo = new VkMemoryAllocateInfo
            {
                SType           = VkStructureType.MemoryAllocateInfo,
                AllocationSize  = requirements.Size,
                MemoryTypeIndex = VulkanBuffers.FindMemoryType(
                    _physicalDevice, requirements.MemoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
            };
            Check(VulkanNative.vkAllocateMemory(_device, in allocateInfo, IntPtr.Zero, out memory),
                  "vkAllocateMemory (image)");
            Check(VulkanNative.vkBindImageMemory(_device, image, memory, 0), "vkBindImageMemory");

            (staging, stagingMemory) = VulkanBuffers.CreateStagingBuffer(_physicalDevice, _device, pixels);
            UploadImage(image, staging, (uint)width, (uint)height);

            var viewInfo = new VkImageViewCreateInfo
            {
                SType    = VkStructureType.ImageViewCreateInfo,
                Image    = image,
                ViewType = VkImageViewType.Type2D,
                Format   = vkFormat,
                SubresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageSubresourceRange.AspectColor,
                    LevelCount = 1,
                    LayerCount = 1,
                },
            };
            Check(VulkanNative.vkCreateImageView(_device, in viewInfo, IntPtr.Zero, out view),
                  "vkCreateImageView (texture)");

            ulong setLayout = _descriptorSetLayout;
            var setAllocInfo = new VkDescriptorSetAllocateInfo
            {
                SType              = VkStructureType.DescriptorSetAllocateInfo,
                DescriptorPool     = _descriptorPool,
                DescriptorSetCount = 1,
                SetLayouts         = (IntPtr)(&setLayout),
            };
            Check(VulkanNative.vkAllocateDescriptorSets(_device, in setAllocInfo, out set),
                  "vkAllocateDescriptorSets");

            var imageDescriptor = new VkDescriptorImageInfo
            {
                Sampler     = _sampler,
                ImageView   = view,
                ImageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            };
            var write = new VkWriteDescriptorSet
            {
                SType           = VkStructureType.WriteDescriptorSet,
                DstSet          = set,
                DstBinding      = 0,
                DescriptorCount = 1,
                DescriptorType  = VkDescriptorType.CombinedImageSampler,
                ImageInfo       = (IntPtr)(&imageDescriptor),
            };
            VulkanNative.vkUpdateDescriptorSets(_device, 1, in write, 0, IntPtr.Zero);

            return new VulkanTexture(this, _device, _descriptorPool, image, memory, view, set, width, height);
        }
        catch
        {
            if (set != 0)    VulkanNative.vkFreeDescriptorSets(_device, _descriptorPool, 1, in set);
            if (view != 0)   VulkanNative.vkDestroyImageView(_device, view, IntPtr.Zero);
            if (image != 0)  VulkanNative.vkDestroyImage(_device, image, IntPtr.Zero);
            if (memory != 0) VulkanNative.vkFreeMemory(_device, memory, IntPtr.Zero);
            throw;
        }
        finally
        {
            if (staging != 0)       VulkanNative.vkDestroyBuffer(_device, staging, IntPtr.Zero);
            if (stagingMemory != 0) VulkanNative.vkFreeMemory(_device, stagingMemory, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Records and submits a one-shot command buffer: transition to TransferDst,
    /// copy the staging buffer into the image, transition to ShaderReadOnly. Waits
    /// the queue idle — the rare, expensive path, like a mesh upload's stall.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan call failed.</exception>
    private unsafe void UploadImage(ulong image, ulong staging, uint width, uint height)
    {
        var allocInfo = new VkCommandBufferAllocateInfo
        {
            SType              = VkStructureType.CommandBufferAllocateInfo,
            CommandPool        = _commandPool,
            Level              = 0,
            CommandBufferCount = 1,
        };
        Check(VulkanNative.vkAllocateCommandBuffers(_device, in allocInfo, out var cmd),
              "vkAllocateCommandBuffers (upload)");

        try
        {
            var beginInfo = new VkCommandBufferBeginInfo
            {
                SType = VkStructureType.CommandBufferBeginInfo,
                Flags = (uint)VkCommandBufferUsageFlags.OneTimeSubmit,
            };
            Check(VulkanNative.vkBeginCommandBuffer(cmd, in beginInfo), "vkBeginCommandBuffer (upload)");

            RecordImageTransition(cmd, image,
                VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
                VkAccessFlags.None, VkAccessFlags.TransferWrite,
                VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer);

            var region = new VkBufferImageCopy
            {
                ImageSubresource = new VkImageSubresourceLayers
                {
                    AspectMask = VkImageSubresourceRange.AspectColor,
                    LayerCount = 1,
                },
                ImageExtent = new VkExtent3D { Width = width, Height = height, Depth = 1 },
            };
            VulkanNative.vkCmdCopyBufferToImage(cmd, staging, image, VkImageLayout.TransferDstOptimal, 1, in region);

            RecordImageTransition(cmd, image,
                VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal,
                VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead,
                VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader);

            Check(VulkanNative.vkEndCommandBuffer(cmd), "vkEndCommandBuffer (upload)");

            IntPtr cmdHandle = cmd;
            var submit = new VkSubmitInfo
            {
                SType              = VkStructureType.SubmitInfo,
                CommandBufferCount = 1,
                CommandBuffers     = (IntPtr)(&cmdHandle),
            };
            Check(VulkanNative.vkQueueSubmit(GraphicsQueue, 1, in submit, 0), "vkQueueSubmit (upload)");
            Check(VulkanNative.vkQueueWaitIdle(GraphicsQueue), "vkQueueWaitIdle (upload)");
        }
        finally
        {
            IntPtr cmdHandle = cmd;
            VulkanNative.vkFreeCommandBuffers(_device, _commandPool, 1, in cmdHandle);
        }
    }

    /// <summary>Disposes the default texture and destroys the sampler, pool and set layout. Tolerates partial construction.</summary>
    private void DestroyTextureMachinery()
    {
        _defaultTexture?.Dispose();
        if (_sampler != 0)
            VulkanNative.vkDestroySampler(_device, _sampler, IntPtr.Zero);
        if (_descriptorPool != 0)
            VulkanNative.vkDestroyDescriptorPool(_device, _descriptorPool, IntPtr.Zero);
        if (_descriptorSetLayout != 0)
            VulkanNative.vkDestroyDescriptorSetLayout(_device, _descriptorSetLayout, IntPtr.Zero);
    }

    /// <summary>Throws if <paramref name="result"/> is an error (negative VkResult).</summary>
    private static void Check(VkResult result, string operation)
    {
        if (result < 0)
            throw new InvalidOperationException($"{operation} failed: {result}.");
    }
}
