using HumbleEngine.OpenGL;
using HumbleEngine.Wayland;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.HAL;

/// <summary>
/// Unit tests for OpenGLGraphicsBackend that require no display or system resources.
/// </summary>
public sealed class OpenGLUnitTests
{
    [Fact]
    public void Name_IsOpenGL()
    {
        using var backend = new OpenGLGraphicsBackend();
        Assert.Equal("OpenGL", backend.Name);
    }

    [Fact]
    public void CompatibleWindowBackends_ContainsX11AndWayland()
    {
        using var backend = new OpenGLGraphicsBackend();
        Assert.Contains(typeof(X11WindowBackend),     backend.CompatibleWindowBackends);
        Assert.Contains(typeof(WaylandWindowBackend), backend.CompatibleWindowBackends);
    }

    [Fact]
    public void Supports_X11Backend_ReturnsTrue()
    {
        using var backend       = new OpenGLGraphicsBackend();
        using var windowBackend = new X11WindowBackend();
        Assert.True(((IGraphicsBackend)backend).Supports(windowBackend));
    }

    [Fact]
    public void Supports_UnknownBackend_ReturnsFalse()
    {
        using var backend = new OpenGLGraphicsBackend();
        Assert.False(((IGraphicsBackend)backend).Supports(new FakeWindowBackend()));
    }

    [Fact]
    public void CreateRenderer_BeforeInitialize_Throws()
    {
        using var backend = new OpenGLGraphicsBackend();
        Assert.Throws<InvalidOperationException>(
            () => backend.CreateRenderer(new FakeSurface()));
    }

    // --- Fakes ---

    private sealed class FakeSurface : IGraphicsSurface
    {
        public bool ShouldClose => false;
        public event Action? OnClose { add { } remove { } }
        public void Run(Action onFrame) { }
        public void Dispose() { }
    }

    private sealed class FakeWindowBackend : IWindowBackend
    {
        public string Name => "FakeWindow";
        public void Initialize() { }
        public IWindow CreateWindow(WindowDescription description) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
