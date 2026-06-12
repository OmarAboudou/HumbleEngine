using HumbleEngine.Tests.Infrastructure;

namespace HumbleEngine.Tests;

/// <summary>
/// Registers FakeDesktopOS once per test run so OS contract tests can run
/// without any platform assembly. Lives in the root test namespace so the
/// one-time setup covers the whole assembly.
/// </summary>
[SetUpFixture]
public sealed class FakeOSSetup
{
    [OneTimeSetUp]
    public void RegisterFakeOS() => OS.Register(new FakeDesktopOS());
}
