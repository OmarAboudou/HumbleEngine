using System.Runtime.InteropServices;

namespace HumbleEngine.Vulkan;

/// <summary>
/// Builds the triangle's graphics pipeline: the complete assembly-line
/// configuration — shaders, topology, rasterizer, blending, dynamic state and
/// the colour attachment format — baked once into an immutable PSO.
/// SPIR-V bytecode is loaded from embedded resources (compiled from
/// <c>Shaders/*.vert|frag</c> at build time by glslangValidator).
/// </summary>
internal static unsafe class VulkanPipeline
{
    /// <summary>
    /// Creates the (empty) pipeline layout and the triangle pipeline targeting
    /// the given colour format. Shader modules are destroyed before returning —
    /// once the pipeline is compiled, the bytecode containers serve no purpose.
    /// Viewport and scissor are dynamic: the pipeline survives window resizes.
    /// </summary>
    /// <exception cref="InvalidOperationException">A Vulkan object could not be created.</exception>
    internal static (ulong Pipeline, ulong Layout) CreateTrianglePipeline(IntPtr device, VkFormat colorFormat)
    {
        var vertModule = CreateShaderModule(device, "Shaders/triangle.vert.spv");
        ulong fragModule = 0;
        ulong layout = 0;

        try
        {
            fragModule = CreateShaderModule(device, "Shaders/triangle.frag.spv");

            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                SType = VkStructureType.PipelineLayoutCreateInfo,
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

                // No vertex input: the vertex shader feeds itself (gl_VertexIndex).
                var vertexInput = new VkPipelineVertexInputStateCreateInfo
                {
                    SType = VkStructureType.PipelineVertexInputStateCreateInfo,
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
