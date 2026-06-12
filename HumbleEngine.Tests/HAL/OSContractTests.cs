using HumbleEngine.Tests.Infrastructure;

namespace HumbleEngine.Tests.HAL;

/// <summary>
/// Tests the OS registration contract using a fake platform — no real OS needed.
/// </summary>
public sealed class OSContractTests
{
    [Test]
    public void Register_Twice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => OS.Register(new FakeDesktopOS()));
    }

    [Test]
    public void Current_IsDesktopOS()
    {
        Assert.That(OS.Current, Is.InstanceOf<DesktopOS>());
    }

    [Test]
    public void GetGraphicsBackend_UnknownName_Throws()
    {
        Assert.Throws<KeyNotFoundException>(
            () => OS.Current.GetGraphicsBackend("NonExistent"));
    }

    [Test]
    public void GetWindowBackend_UnknownName_Throws()
    {
        Assert.Throws<KeyNotFoundException>(
            () => ((DesktopOS)OS.Current).GetWindowBackend("NonExistent"));
    }
}
