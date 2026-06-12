using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

public sealed class X11WindowTests
{
    private X11WindowBackend _backend = null!;
    private IWindow _window = null!;

    [SetUp]
    public void OpenWindow()
    {
        _backend = new X11WindowBackend();
        _backend.Initialize();
        _window = _backend.CreateWindow(new WindowDescription("Test", 100, 100));
    }

    [TearDown]
    public void CloseWindow()
    {
        _window.Dispose();
        _backend.Dispose();
    }

    [Test]
    public void GetNativeHandle_ReturnsNonZero()
    {
        var handle = (INativeWindowHandle)_window;
        Assert.That(handle.GetNativeHandle(), Is.Not.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void GetConnectionHandle_ReturnsNonNull()
    {
        var handle = (INativeWindowHandle)_window;
        Assert.That(handle.GetConnectionHandle(), Is.Not.EqualTo(IntPtr.Zero));
    }
}
