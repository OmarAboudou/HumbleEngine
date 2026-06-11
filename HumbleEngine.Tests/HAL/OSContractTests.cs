using HumbleEngine.Tests.Infrastructure;

namespace HumbleEngine.Tests.HAL;

/// <summary>
/// Tests the OS registration contract using a fake platform — no real OS needed.
/// </summary>
[Collection("FakeOS")]
public sealed class OSContractTests
{
    [Fact]
    public void Register_Twice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => OS.Register(new FakeDesktopOS()));
    }

    [Fact]
    public void Current_IsDesktopOS()
    {
        Assert.IsAssignableFrom<DesktopOS>(OS.Current);
    }

    [Fact]
    public void GetGraphicsBackend_UnknownName_Throws()
    {
        Assert.Throws<KeyNotFoundException>(
            () => OS.Current.GetGraphicsBackend("NonExistent"));
    }

    [Fact]
    public void GetWindowBackend_UnknownName_Throws()
    {
        Assert.Throws<KeyNotFoundException>(
            () => ((DesktopOS)OS.Current).GetWindowBackend("NonExistent"));
    }
}
