using HumbleEngine.Vulkan;
using HumbleEngine.Wayland;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

/// <summary>
/// Integration tests for the Vulkan backend.
/// Bloc 1 (instance + physical device) requires only the Vulkan loader and an ICD;
/// bloc 2 (renderer: surface + device + swapchain) also requires a display connection.
/// </summary>
public sealed class VulkanIntegrationTests
{
    [Test]
    public void Initialize_Succeeds()
    {
        using var backend = new VulkanGraphicsBackend();
        backend.Initialize();
    }

    [Test]
    public void Initialize_IsIdempotent()
    {
        using var backend = new VulkanGraphicsBackend();
        backend.Initialize();
        backend.Initialize();
    }

    [Test]
    public void Initialize_SelectsAGpu()
    {
        using var backend = new VulkanGraphicsBackend();
        backend.Initialize();

        Assert.That(string.IsNullOrWhiteSpace(backend.DeviceName), Is.False);
    }

    [Test]
    public void DeviceName_BeforeInitialize_IsNull()
    {
        using var backend = new VulkanGraphicsBackend();
        Assert.That(backend.DeviceName, Is.Null);
    }

    [Test]
    public void CreateRenderer_BeforeInitialize_Throws()
    {
        using var backend       = new VulkanGraphicsBackend();
        using var windowBackend = new X11WindowBackend();
        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        Assert.Throws<InvalidOperationException>(() => backend.CreateRenderer(window));
    }

    [Test]
    public void Dispose_IsIdempotent()
    {
        var backend = new VulkanGraphicsBackend();
        backend.Initialize();
        backend.Dispose();
        backend.Dispose();
    }

    [Test]
    public void Dispose_ThenInitialize_Succeeds()
    {
        using var backend = new VulkanGraphicsBackend();
        backend.Initialize();
        backend.Dispose();
        backend.Initialize();

        Assert.That(string.IsNullOrWhiteSpace(backend.DeviceName), Is.False);
    }

    // --- Bloc 2 : surface + device logique + swapchain ---

    [Test]
    public void CreateRenderer_OnX11Window_ReturnsRenderer()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        Assert.That(renderer, Is.Not.Null);
    }

    [Test]
    public void CreateRenderer_OnWaylandWindow_ReturnsRenderer()
    {
        using var windowBackend   = new WaylandWindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        Assert.That(renderer, Is.Not.Null);
    }

    [Test]
    public void CreateRenderer_WithSurfaceWithoutNativeHandle_Throws()
    {
        using var backend = new VulkanGraphicsBackend();
        backend.Initialize();

        Assert.Throws<ArgumentException>(() => backend.CreateRenderer(new FakeSurface()));
    }

    [Test]
    public void Renderer_Dispose_IsIdempotent()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        var renderer = graphicsBackend.CreateRenderer(window);

        renderer.Dispose();
        renderer.Dispose();
    }

    // --- Bloc 3 : cycle de frame ---

    [Test]
    public void Renderer_FrameCycle_OnX11_DoesNotThrow()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        for (int i = 0; i < 3; i++)
        {
            renderer.BeginFrame();
            renderer.EndFrame();
            renderer.Present();
        }
    }

    [Test]
    public void Renderer_FrameCycle_OnWayland_DoesNotThrow()
    {
        using var windowBackend   = new WaylandWindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        for (int i = 0; i < 3; i++)
        {
            renderer.BeginFrame();
            renderer.EndFrame();
            renderer.Present();
        }
    }

    // --- Bloc 4 roadmap 07 : meshes + dessin piloté par l'arbre ---

    private static readonly Vertex[] Triangle =
    [
        new(new Vector2( 0.0f, -0.5f), new Vector3(1f, 0f, 0f)),
        new(new Vector2( 0.5f,  0.5f), new Vector3(0f, 1f, 0f)),
        new(new Vector2(-0.5f,  0.5f), new Vector3(0f, 0f, 1f)),
    ];

    [Test]
    public void CreateMesh_ReturnsDisposableMesh()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        var mesh = renderer.CreateMesh(Triangle);
        Assert.That(mesh, Is.Not.Null);
        mesh.Dispose();
        mesh.Dispose(); // idempotent
    }

    [Test]
    public void Renderer_DrawsMesh_OverSeveralFrames()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);
        using var mesh = renderer.CreateMesh(Triangle);

        for (int i = 0; i < 3; i++)
        {
            renderer.BeginFrame();
            renderer.Draw(mesh);
            renderer.EndFrame();
            renderer.Present();
        }
    }

    [Test]
    public void Draw_OutsideFrame_Throws()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);
        using var mesh = renderer.CreateMesh(Triangle);

        Assert.Throws<InvalidOperationException>(() => renderer.Draw(mesh));
    }

    [Test]
    public void Draw_WithForeignMesh_Throws()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        renderer.BeginFrame();
        try
        {
            Assert.Throws<ArgumentException>(() => renderer.Draw(new ForeignMesh()));
        }
        finally
        {
            renderer.EndFrame();
            renderer.Present();
        }
    }

    [Test]
    public void Mesh_DisposedMidLoop_FramesKeepRendering()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);
        var mesh = renderer.CreateMesh(Triangle);

        renderer.BeginFrame();
        renderer.Draw(mesh);
        renderer.EndFrame();
        renderer.Present();

        // The Sandbox scenario: the mesh dies between two frames, while its
        // last draw may still be in flight — VulkanMesh waits the device idle.
        mesh.Dispose();

        renderer.BeginFrame();
        renderer.EndFrame();
        renderer.Present();
    }

    [Test]
    public void SceneTree_DrivesTheVulkanRenderer_EndToEnd()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        var node = new MeshNode();
        using var tree = new SceneTree(renderer) { Root = node };

        // Frame N: the tree submits the draw; the node then queues its death.
        renderer.BeginFrame();
        tree.Render();
        renderer.EndFrame();
        renderer.Present();
        node.QueueDispose();
        tree.FlushDisposeQueue();

        // Frame N+1: empty tree, the mesh is gone, the frame still renders.
        renderer.BeginFrame();
        tree.Render();
        renderer.EndFrame();
        renderer.Present();
    }

    [Test]
    public void Draw_WithMeshFromAnotherRenderer_Throws()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var windowA = windowBackend.CreateWindow(new WindowDescription("A", 100, 100));
        using var windowB = windowBackend.CreateWindow(new WindowDescription("B", 100, 100));

        graphicsBackend.Initialize();
        using var rendererA = graphicsBackend.CreateRenderer(windowA);
        using var rendererB = graphicsBackend.CreateRenderer(windowB);
        using var meshFromA = rendererA.CreateMesh(Triangle);

        // The origin guard: a mesh's buffer lives on its creator's device —
        // mixing renderers must be a clear exception, not a Vulkan crash.
        rendererB.BeginFrame();
        try
        {
            Assert.Throws<ArgumentException>(() => rendererB.Draw(meshFromA));
        }
        finally
        {
            rendererB.EndFrame();
            rendererB.Present();
        }
    }

    // --- Bloc 5 roadmap 08 : le chemin quad (übershader UI) ---

    [Test]
    public void Renderer_DrawsQuadsAndMesh_InTheSameFrame()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);
        using var mesh = renderer.CreateMesh(Triangle);

        // Quad → mesh → quad: the lazy pipeline binding switches twice.
        for (int i = 0; i < 3; i++)
        {
            renderer.BeginFrame();
            renderer.DrawQuad(new Rect(10f, 10f, 50f, 30f), new Vector4(1f, 0f, 0f, 0.5f));
            renderer.Draw(mesh);
            renderer.DrawQuad(new Rect(20f, 40f, 50f, 30f), new Vector4(0f, 1f, 0f, 1f));
            renderer.EndFrame();
            renderer.Present();
        }
    }

    [Test]
    public void DrawQuad_OutsideFrame_Throws()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new VulkanGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        Assert.Throws<InvalidOperationException>(
            () => renderer.DrawQuad(new Rect(0f, 0f, 10f, 10f), new Vector4(1f, 1f, 1f, 1f)));
    }

    [Test]
    public void Window_ReportsItsSize()
    {
        using var windowBackend = new X11WindowBackend();
        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 320, 240));

        Assert.That(window.Width, Is.EqualTo(320));
        Assert.That(window.Height, Is.EqualTo(240));
    }

    // --- Fakes ---

    /// <summary>An <see cref="IMesh"/> that no Vulkan renderer ever created.</summary>
    private sealed class ForeignMesh : IMesh
    {
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// The Sandbox triangle's shape, replayed against the real backend:
    /// default-constructible, mesh acquired from the context renderer on
    /// attach, released on detach (roadmap 09).
    /// </summary>
    private sealed class MeshNode : VisualNode
    {
        private IMesh? _mesh;

        protected override void OnAttached() => _mesh = Renderer!.CreateMesh(Triangle);

        protected override void OnDetached()
        {
            _mesh?.Dispose();
            _mesh = null;
        }

        protected override void OnDraw(IRenderer renderer) => renderer.Draw(_mesh!);
    }

    private sealed class FakeSurface : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public bool Step(Action onFrame) => false;
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }
}
