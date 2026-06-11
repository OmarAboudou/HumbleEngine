using HumbleEngine.Tests.Linux.Infrastructure;
using HumbleEngine.Wayland;

namespace HumbleEngine.Tests.Linux.HAL;

/// <summary>
/// Integration tests for the Wayland backend. Require a running Wayland compositor
/// (<c>WAYLAND_DISPLAY</c> set) — they exercise both decoration paths:
/// libdecor (default) and raw XDG Shell (<see cref="WindowDescription.Borderless"/>).
/// </summary>
[Collection("Linux")]
public sealed class WaylandWindowTests
{
    // --- Backend lifecycle ---

    [Fact]
    public void Initialize_Succeeds()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        backend.Initialize();
    }

    [Fact]
    public void CreateWindow_BeforeInitialize_Throws()
    {
        using var backend = new WaylandWindowBackend();
        Assert.Throws<InvalidOperationException>(
            () => backend.CreateWindow(new WindowDescription("Test", 100, 100)));
    }

    // --- Decorated window (libdecor path when libdecor-0 is installed) ---

    [Fact]
    public void CreateWindow_Decorated_ReturnsWindow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        Assert.NotNull(window);
    }

    [Fact]
    public void GetNativeHandle_Decorated_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        var handle = (INativeWindowHandle)window;
        Assert.NotEqual(IntPtr.Zero, handle.GetNativeHandle());
    }

    [Fact]
    public void GetConnectionHandle_Decorated_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        var handle = (INativeWindowHandle)window;
        Assert.NotEqual(IntPtr.Zero, handle.GetConnectionHandle());
    }

    [Fact]
    public void SetTitle_Decorated_DoesNotThrow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        window.SetTitle("Nouveau titre");
    }

    // --- Borderless window (raw XDG Shell path, no libdecor) ---

    [Fact]
    public void CreateWindow_Borderless_ReturnsWindow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        Assert.NotNull(window);
    }

    [Fact]
    public void GetNativeHandle_Borderless_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        var handle = (INativeWindowHandle)window;
        Assert.NotEqual(IntPtr.Zero, handle.GetNativeHandle());
    }

    [Fact]
    public void SetTitle_Borderless_DoesNotThrow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        window.SetTitle("Nouveau titre");
    }

    // --- Multiple windows / disposal ---

    [Fact]
    public void CreateWindow_Twice_BothValid()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var first  = backend.CreateWindow(new WindowDescription("First", 100, 100));
        using var second = backend.CreateWindow(new WindowDescription("Second", 100, 100));

        Assert.NotEqual(
            ((INativeWindowHandle)first).GetNativeHandle(),
            ((INativeWindowHandle)second).GetNativeHandle());
    }

    [Fact]
    public void Window_Dispose_IsIdempotent()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        window.Dispose();
        window.Dispose();
    }

    [Fact]
    public void Backend_Dispose_IsIdempotent()
    {
        var backend = new WaylandWindowBackend();
        backend.Initialize();

        backend.Dispose();
        backend.Dispose();
    }
}
