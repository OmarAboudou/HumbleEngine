using HumbleEngine.Linux;

namespace HumbleEngine.Tests.Linux;

/// <summary>
/// Registers LinuxOS once for the entire integration test session. Lives in the
/// root test namespace so the one-time setup covers the whole assembly.
/// </summary>
[SetUpFixture]
public sealed class LinuxSetup
{
    [OneTimeSetUp]
    public void RegisterLinuxOS() => OS.Register(new LinuxOS());
}
