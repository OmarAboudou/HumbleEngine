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

    private sealed class FakeSurface : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }
}
