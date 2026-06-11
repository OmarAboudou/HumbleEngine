namespace HumbleEngine.Tests.Infrastructure;

/// <summary>
/// Registers FakeDesktopOS once per test process so OS contract tests can run
/// without any platform assembly.
/// </summary>
public sealed class FakeOSFixture
{
    public FakeOSFixture() => OS.Register(new FakeDesktopOS());
}

[CollectionDefinition("FakeOS")]
public sealed class FakeOSCollection : ICollectionFixture<FakeOSFixture> { }
