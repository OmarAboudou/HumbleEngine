using HumbleEngine.OpenGL;
using HumbleEngine.Tests.Linux.Infrastructure;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

[Collection("Linux")]
public sealed class OpenGLIntegrationTests
{
    [Fact]
    public void Initialize_Succeeds()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        backend.Initialize();
    }

    [Fact]
    public void CreateRenderer_BeforeInitialize_Throws()
    {
        using var backend       = new OpenGLGraphicsBackend();
        using var windowBackend = new X11WindowBackend();
        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        Assert.Throws<InvalidOperationException>(() => backend.CreateRenderer(window));
    }

    [Fact]
    public void CreateRenderer_WithSurfaceWithoutNativeHandle_Throws()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        Assert.Throws<ArgumentException>(() => backend.CreateRenderer(new FakeSurface()));
    }

    [Fact]
    public void CreateRenderer_WithNullConnectionHandle_Throws()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        Assert.Throws<ArgumentException>(
            () => backend.CreateRenderer(new FakeSurfaceWithHandle(IntPtr.Zero, IntPtr.Zero)));
    }

    [Fact]
    public void CreateRenderer_ReturnsRenderer()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new OpenGLGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        Assert.NotNull(renderer);
    }

    [Fact]
    public void Renderer_FrameCycle_DoesNotThrow()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new OpenGLGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        renderer.BeginFrame();
        renderer.EndFrame();
        renderer.Present();
    }

    [Fact]
    public void Renderer_Dispose_IsIdempotent()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new OpenGLGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        var renderer = graphicsBackend.CreateRenderer(window);

        renderer.Dispose();
        renderer.Dispose();
    }

    // --- Fakes ---

    private sealed class FakeSurface : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }

    private class FakeSurfaceBase : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }

    private sealed class FakeSurfaceWithHandle : FakeSurfaceBase, INativeWindowHandle
    {
        private readonly IntPtr _connection;
        private readonly IntPtr _window;

        public FakeSurfaceWithHandle(IntPtr connection, IntPtr window)
        {
            _connection = connection;
            _window     = window;
        }

        public IntPtr GetNativeHandle()     => _window;
        public IntPtr GetConnectionHandle() => _connection;
    }
}
