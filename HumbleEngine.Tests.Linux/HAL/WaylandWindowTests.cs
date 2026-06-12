using HumbleEngine.Wayland;

namespace HumbleEngine.Tests.Linux.HAL;

/// <summary>
/// Integration tests for the Wayland backend. Require a running Wayland compositor
/// (<c>WAYLAND_DISPLAY</c> set) — they exercise both decoration paths:
/// libdecor (default) and raw XDG Shell (<see cref="WindowDescription.Borderless"/>).
/// </summary>
public sealed class WaylandWindowTests
{
    // --- Backend lifecycle ---

    [Test]
    public void Initialize_Succeeds()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
    }

    [Test]
    public void Initialize_IsIdempotent()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        backend.Initialize();
    }

    [Test]
    public void CreateWindow_BeforeInitialize_Throws()
    {
        using var backend = new WaylandWindowBackend();
        Assert.Throws<InvalidOperationException>(
            () => backend.CreateWindow(new WindowDescription("Test", 100, 100)));
    }

    // --- Decorated window (libdecor path when libdecor-0 is installed) ---

    [Test]
    public void CreateWindow_Decorated_ReturnsWindow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        Assert.That(window, Is.Not.Null);
    }

    [Test]
    public void GetNativeHandle_Decorated_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        var handle = (INativeWindowHandle)window;
        Assert.That(handle.GetNativeHandle(), Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void GetConnectionHandle_Decorated_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        var handle = (INativeWindowHandle)window;
        Assert.That(handle.GetConnectionHandle(), Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void SetTitle_Decorated_DoesNotThrow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        window.SetTitle("Nouveau titre");
    }

    // --- Borderless window (raw XDG Shell path, no libdecor) ---

    [Test]
    public void CreateWindow_Borderless_ReturnsWindow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        Assert.That(window, Is.Not.Null);
    }

    [Test]
    public void GetNativeHandle_Borderless_ReturnsNonZero()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        var handle = (INativeWindowHandle)window;
        Assert.That(handle.GetNativeHandle(), Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void SetTitle_Borderless_DoesNotThrow()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var window = backend.CreateWindow(
            new WindowDescription("Test", 100, 100) { Borderless = true });

        window.SetTitle("Nouveau titre");
    }

    // --- Multiple windows / disposal ---

    [Test]
    public void CreateWindow_Twice_BothValid()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        using var first  = backend.CreateWindow(new WindowDescription("First", 100, 100));
        using var second = backend.CreateWindow(new WindowDescription("Second", 100, 100));

        Assert.That(
            ((INativeWindowHandle)second).GetNativeHandle(),
            Is.Not.EqualTo(((INativeWindowHandle)first).GetNativeHandle()));
    }

    [Test]
    public void Window_Dispose_IsIdempotent()
    {
        using var backend = new WaylandWindowBackend();
        backend.Initialize();
        var window = backend.CreateWindow(new WindowDescription("Test", 100, 100));

        window.Dispose();
        window.Dispose();
    }

    [Test]
    public void Backend_Dispose_IsIdempotent()
    {
        var backend = new WaylandWindowBackend();
        backend.Initialize();

        backend.Dispose();
        backend.Dispose();
    }
}
