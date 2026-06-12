using HumbleEngine.OpenGL;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

public sealed class OpenGLIntegrationTests
{
    [Test]
    public void Initialize_Succeeds()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
    }

    [Test]
    public void Initialize_IsIdempotent()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        backend.Initialize();
    }

    [Test]
    public void CreateRenderer_BeforeInitialize_Throws()
    {
        using var backend       = new OpenGLGraphicsBackend();
        using var windowBackend = new X11WindowBackend();
        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        Assert.Throws<InvalidOperationException>(() => backend.CreateRenderer(window));
    }

    [Test]
    public void CreateRenderer_WithSurfaceWithoutNativeHandle_Throws()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        Assert.Throws<ArgumentException>(() => backend.CreateRenderer(new FakeSurface()));
    }

    [Test]
    public void CreateRenderer_WithNullConnectionHandle_Throws()
    {
        using var backend = new OpenGLGraphicsBackend();
        backend.Initialize();
        Assert.Throws<ArgumentException>(
            () => backend.CreateRenderer(new FakeSurfaceWithHandle(IntPtr.Zero, IntPtr.Zero)));
    }

    [Test]
    public void CreateRenderer_ReturnsRenderer()
    {
        using var windowBackend   = new X11WindowBackend();
        using var graphicsBackend = new OpenGLGraphicsBackend();

        windowBackend.Initialize();
        using var window = windowBackend.CreateWindow(new WindowDescription("Test", 100, 100));

        graphicsBackend.Initialize();
        using var renderer = graphicsBackend.CreateRenderer(window);

        Assert.That(renderer, Is.Not.Null);
    }

    [Test]
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

    [Test]
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
        public bool Step(Action onFrame) => false;
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }

    private class FakeSurfaceBase : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public bool Step(Action onFrame) => false;
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
