using HumbleEngine.Linux;
using HumbleEngine.OpenGL;
using HumbleEngine.X11;

namespace HumbleEngine.Tests.Linux.HAL;

public sealed class OSLinuxTests
{
    [Test]
    public void Current_IsLinuxOS()
    {
        Assert.That(OS.Current, Is.TypeOf<LinuxOS>());
        Assert.That(OS.Current.Name, Is.EqualTo("Linux"));
    }

    [Test]
    public void Current_IsDesktopOS()
    {
        Assert.That(OS.Current, Is.InstanceOf<DesktopOS>());
    }

    [Test]
    public void GetWindowBackend_X11_ReturnsX11Backend()
    {
        var backend = ((DesktopOS)OS.Current).GetWindowBackend("X11");
        Assert.That(backend, Is.TypeOf<X11WindowBackend>());
    }

    [Test]
    public void GetGraphicsBackend_OpenGL_ReturnsOpenGLBackend()
    {
        var backend = OS.Current.GetGraphicsBackend("OpenGL");
        Assert.That(backend, Is.TypeOf<OpenGLGraphicsBackend>());
    }
}
