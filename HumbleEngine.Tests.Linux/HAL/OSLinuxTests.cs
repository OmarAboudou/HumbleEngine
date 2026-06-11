using HumbleEngine.Linux;
using HumbleEngine.OpenGL;
using HumbleEngine.Tests.Linux.Infrastructure;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

[Collection("Linux")]
public sealed class OSLinuxTests
{
    [Fact]
    public void Current_IsLinuxOS()
    {
        Assert.IsType<LinuxOS>(OS.Current);
        Assert.Equal("Linux", OS.Current.Name);
    }

    [Fact]
    public void Current_IsDesktopOS()
    {
        Assert.IsAssignableFrom<DesktopOS>(OS.Current);
    }

    [Fact]
    public void GetWindowBackend_X11_ReturnsX11Backend()
    {
        var backend = ((DesktopOS)OS.Current).GetWindowBackend("X11");
        Assert.IsType<X11WindowBackend>(backend);
    }

    [Fact]
    public void GetGraphicsBackend_OpenGL_ReturnsOpenGLBackend()
    {
        var backend = OS.Current.GetGraphicsBackend("OpenGL");
        Assert.IsType<OpenGLGraphicsBackend>(backend);
    }
}
