using System.Runtime.InteropServices;

namespace HumbleEngine.Vulkan;

/// <summary>
/// Push-constant geography of the UI übershader — one 128-byte block (the
/// minimum the standard guarantees), two write cadences: the orthographic
/// matrix once per frame, the quad parameters once per draw. Push constants
/// persist across draws of a command buffer, so the matrix outlives every quad.
/// </summary>
internal static class QuadPush
{
    /// <summary>Pixels → clip matrix: offset 0, written once per frame, vertex stage.</summary>
    internal const uint MatrixOffset = 0;

    /// <summary>Size of the matrix portion (a raw <see cref="Matrix4x4"/> copy).</summary>
    internal const uint MatrixSize = 64;

    /// <summary>Per-draw parameters: offset just after the matrix.</summary>
    internal const uint ParamsOffset = MatrixSize;

    /// <summary>Size of <see cref="QuadParams"/>.</summary>
    internal const uint ParamsSize = 64;

    /// <summary>Whole block, declared as a single vertex+fragment range.</summary>
    internal const uint TotalSize = MatrixSize + ParamsSize;
}

/// <summary>
/// Per-draw half of the übershader push block, laid out exactly as the GLSL
/// declaration (vec4 rect, vec4 uvRect, vec4 color, int mode — tail padded to 16).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct QuadParams(Vector4 rect, Vector4 uvRect, Vector4 color, int mode)
{
    /// <summary>x, y, width, height — pixels, origin top-left.</summary>
    public readonly Vector4 Rect = rect;

    /// <summary>u, v, uw, vh — the sampled sub-rectangle of the texture, 0..1 (flat: 0,0,1,1).</summary>
    public readonly Vector4 UvRect = uvRect;

    /// <summary>RGBA colour, alpha blended — the tint in textured/glyph modes.</summary>
    public readonly Vector4 Color = color;

    /// <summary>Fragment-side interpretation — 0: flat, 1: textured, 2: glyph.</summary>
    public readonly int Mode = mode;

    private readonly int _pad0, _pad1, _pad2;
}

/// <summary>
/// Builds the engine's graphics pipelines, each a complete assembly-line
/// configuration baked once into an immutable PSO: the <b>mesh</b> pipeline
/// (vertex buffer input, opaque) and the <b>quad</b> pipeline (the UI
/// übershader: no vertex input, push constants, alpha blending).
/// SPIR-V bytecode is loaded from embedded resources (compiled from
/// <c>Shaders/*.vert|frag</c> at build time by glslangValidator).
/// </summary>
internal static unsafe class VulkanPipeline
{
    /// <summary>
    /// Creates the mesh pipeline: one vertex stream of HAL <see cref="Vertex"/>
    /// (Vector2 position at location 0, Vector3 colour at location 1 — the typed
    /// contract matching the shader's <c>layout(location = N) in</c>), no
    /// blending, empty layout.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan object could not be created.</exception>
    internal static (ulong Pipeline, ulong Layout) CreateMeshPipeline(IntPtr device, VkFormat colorFormat)
    {
        var binding = new VkVertexInputBindingDescription
        {
            Binding   = 0,
            Stride    = (uint)sizeof(Vertex),
            InputRate = 0, // per vertex
        };

        var attributes = stackalloc VkVertexInputAttributeDescription[2]
        {
            new() { Location = 0, Binding = 0, Format = VkFormat.R32G32Sfloat, Offset = 0 },
            new() { Location = 1, Binding = 0, Format = VkFormat.R32G32B32Sfloat, Offset = (uint)sizeof(Vector2) },
        };

        return CreatePipeline(
            device, colorFormat,
            "Shaders/triangle.vert.spv", "Shaders/triangle.frag.spv",
            &binding, 1, attributes, 2,
            alphaBlend: false, null, 0, descriptorSetLayout: 0);
    }

    /// <summary>
    /// Creates the quad pipeline (UI übershader): no vertex input at all (the
    /// unit quad is generated from <c>gl_VertexIndex</c>), classic alpha
    /// blending, a single vertex+fragment push-constant range covering the whole
    /// <see cref="QuadPush"/> block, and the descriptor set layout the fragment
    /// shader samples through (set 0, binding 0 — one combined image sampler,
    /// always bound, a default white texture for flat draws).
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan object could not be created.</exception>
    internal static (ulong Pipeline, ulong Layout) CreateQuadPipeline(
        IntPtr device, VkFormat colorFormat, ulong descriptorSetLayout)
    {
        var range = new VkPushConstantRange
        {
            StageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
            Offset     = 0,
            Size       = QuadPush.TotalSize,
        };

        return CreatePipeline(
            device, colorFormat,
            "Shaders/ui.vert.spv", "Shaders/ui.frag.spv",
            null, 0, null, 0,
            alphaBlend: true, &range, 1, descriptorSetLayout);
    }

    /// <summary>
    /// The shared assembly line: layout (with optional push ranges) + PSO —
    /// shaders, vertex input (optional), topology, rasterizer, blending
    /// (opaque or alpha), dynamic viewport/scissor, and the colour attachment
    /// format chained through pNext (dynamic rendering, no render pass).
    /// Shader modules are destroyed before returning — once the pipeline is
    /// compiled, the bytecode containers serve no purpose.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan object could not be created.</exception>
    private static (ulong Pipeline, ulong Layout) CreatePipeline(
        IntPtr device, VkFormat colorFormat,
        string vertResource, string fragResource,
        VkVertexInputBindingDescription* bindings, uint bindingCount,
        VkVertexInputAttributeDescription* attributes, uint attributeCount,
        bool alphaBlend, VkPushConstantRange* pushRanges, uint pushRangeCount,
        ulong descriptorSetLayout)
    {
        var vertModule = CreateShaderModule(device, vertResource);
        ulong fragModule = 0;
        ulong layout = 0;

        try
        {
            fragModule = CreateShaderModule(device, fragResource);

            var setLayout = descriptorSetLayout;
            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                SType                  = VkStructureType.PipelineLayoutCreateInfo,
                SetLayoutCount         = descriptorSetLayout != 0 ? 1u : 0u,
                SetLayouts             = descriptorSetLayout != 0 ? (IntPtr)(&setLayout) : IntPtr.Zero,
                PushConstantRangeCount = pushRangeCount,
                PushConstantRanges     = (IntPtr)pushRanges,
            };
            Check(VulkanNative.vkCreatePipelineLayout(device, in layoutInfo, IntPtr.Zero, out layout),
                  "vkCreatePipelineLayout");

            var entryPoint = Marshal.StringToHGlobalAnsi("main");
            try
            {
                var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
                stages[0] = new VkPipelineShaderStageCreateInfo
                {
                    SType  = VkStructureType.PipelineShaderStageCreateInfo,
                    Stage  = VkShaderStageFlags.Vertex,
                    Module = vertModule,
                    Name   = entryPoint,
                };
                stages[1] = new VkPipelineShaderStageCreateInfo
                {
                    SType  = VkStructureType.PipelineShaderStageCreateInfo,
                    Stage  = VkShaderStageFlags.Fragment,
                    Module = fragModule,
                    Name   = entryPoint,
                };

                var vertexInput = new VkPipelineVertexInputStateCreateInfo
                {
                    SType                           = VkStructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount   = bindingCount,
                    VertexBindingDescriptions       = (IntPtr)bindings,
                    VertexAttributeDescriptionCount = attributeCount,
                    VertexAttributeDescriptions     = (IntPtr)attributes,
                };

                var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
                {
                    SType    = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = VkPrimitiveTopology.TriangleList,
                };

                var viewportState = new VkPipelineViewportStateCreateInfo
                {
                    SType         = VkStructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    ScissorCount  = 1,
                };

                var rasterization = new VkPipelineRasterizationStateCreateInfo
                {
                    SType       = VkStructureType.PipelineRasterizationStateCreateInfo,
                    PolygonMode = VkPolygonMode.Fill,
                    CullMode    = VkCullModeFlags.None,
                    LineWidth   = 1.0f,
                };

                var multisample = new VkPipelineMultisampleStateCreateInfo
                {
                    SType                = VkStructureType.PipelineMultisampleStateCreateInfo,
                    RasterizationSamples = 1,
                };

                var blendAttachment = new VkPipelineColorBlendAttachmentState
                {
                    ColorWriteMask = VkPipelineColorBlendAttachmentState.WriteAll,
                };
                if (alphaBlend)
                {
                    // Classic "over" compositing: src·α + dst·(1−α).
                    blendAttachment.BlendEnable         = 1;
                    blendAttachment.SrcColorBlendFactor = VkBlendFactor.SrcAlpha;
                    blendAttachment.DstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
                    blendAttachment.ColorBlendOp        = VkBlendOp.Add;
                    blendAttachment.SrcAlphaBlendFactor = VkBlendFactor.One;
                    blendAttachment.DstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
                    blendAttachment.AlphaBlendOp        = VkBlendOp.Add;
                }

                var colorBlend = new VkPipelineColorBlendStateCreateInfo
                {
                    SType           = VkStructureType.PipelineColorBlendStateCreateInfo,
                    AttachmentCount = 1,
                    Attachments     = (IntPtr)(&blendAttachment),
                };

                var dynamicStates = stackalloc VkDynamicState[2]
                {
                    VkDynamicState.Viewport,
                    VkDynamicState.Scissor,
                };
                var dynamicState = new VkPipelineDynamicStateCreateInfo
                {
                    SType             = VkStructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = 2,
                    DynamicStates     = (IntPtr)dynamicStates,
                };

                // Dynamic rendering: the pipeline declares its attachment format
                // itself, chained through pNext — no render pass involved.
                var renderingInfo = new VkPipelineRenderingCreateInfo
                {
                    SType                  = VkStructureType.PipelineRenderingCreateInfo,
                    ColorAttachmentCount   = 1,
                    ColorAttachmentFormats = (IntPtr)(&colorFormat),
                };

                var createInfo = new VkGraphicsPipelineCreateInfo
                {
                    SType              = VkStructureType.GraphicsPipelineCreateInfo,
                    Next               = (IntPtr)(&renderingInfo),
                    StageCount         = 2,
                    Stages             = (IntPtr)stages,
                    VertexInputState   = (IntPtr)(&vertexInput),
                    InputAssemblyState = (IntPtr)(&inputAssembly),
                    ViewportState      = (IntPtr)(&viewportState),
                    RasterizationState = (IntPtr)(&rasterization),
                    MultisampleState   = (IntPtr)(&multisample),
                    ColorBlendState    = (IntPtr)(&colorBlend),
                    DynamicState       = (IntPtr)(&dynamicState),
                    Layout             = layout,
                };

                Check(VulkanNative.vkCreateGraphicsPipelines(
                          device, 0, 1, in createInfo, IntPtr.Zero, out var pipeline),
                      "vkCreateGraphicsPipelines");
                return (pipeline, layout);
            }
            finally
            {
                Marshal.FreeHGlobal(entryPoint);
            }
        }
        catch
        {
            if (layout != 0)
                VulkanNative.vkDestroyPipelineLayout(device, layout, IntPtr.Zero);
            throw;
        }
        finally
        {
            VulkanNative.vkDestroyShaderModule(device, vertModule, IntPtr.Zero);
            if (fragModule != 0)
                VulkanNative.vkDestroyShaderModule(device, fragModule, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Loads an embedded SPIR-V resource and wraps it into a shader module.
    /// </summary>
    /// <exception cref="InvalidOperationException">The resource is missing or the module creation failed.</exception>
    private static ulong CreateShaderModule(IntPtr device, string resourceName)
    {
        using var stream = typeof(VulkanPipeline).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded shader '{resourceName}' not found — was it compiled at build time?");

        var code = new byte[stream.Length];
        stream.ReadExactly(code);

        fixed (byte* codePtr = code)
        {
            var createInfo = new VkShaderModuleCreateInfo
            {
                SType    = VkStructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                Code     = (IntPtr)codePtr,
            };
            Check(VulkanNative.vkCreateShaderModule(device, in createInfo, IntPtr.Zero, out var module),
                  "vkCreateShaderModule");
            return module;
        }
    }

    /// <summary>Throws if <paramref name="result"/> is an error (negative VkResult).</summary>
    private static void Check(VkResult result, string operation)
    {
        if (result < 0)
            throw new InvalidOperationException($"{operation} failed: {result}.");
    }
}
