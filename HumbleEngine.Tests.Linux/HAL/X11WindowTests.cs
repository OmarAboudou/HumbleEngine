using HumbleEngine.Tests.Linux.Infrastructure;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

[Collection("Linux")]
public sealed class X11WindowTests : IDisposable
{
    private readonly X11WindowBackend _backend = new();
    private readonly IWindow _window;

    public X11WindowTests()
    {
        _backend.Initialize();
        _window = _backend.CreateWindow(new WindowDescription("Test", 100, 100));
    }

    [Fact]
    public void GetNativeHandle_ReturnsNonZero()
    {
        var handle = (INativeWindowHandle)_window;
        Assert.NotEqual(IntPtr.Zero, handle.GetNativeHandle());
    }

    [Fact]
    public void GetConnectionHandle_ReturnsNonNull()
    {
        var handle = (INativeWindowHandle)_window;
        Assert.NotEqual(IntPtr.Zero, handle.GetConnectionHandle());
    }

    public void Dispose()
    {
        _window.Dispose();
        _backend.Dispose();
    }
}
