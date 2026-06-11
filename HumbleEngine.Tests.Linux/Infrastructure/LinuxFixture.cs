using HumbleEngine.Linux;

namespace HumbleEngine.Tests.Linux.Infrastructure;

/// <summary>
/// Registers LinuxOS once for the entire integration test session.
/// All tests that open X11 windows or GLX contexts must belong to the "Linux" collection.
/// </summary>
public sealed class LinuxFixture
{
    public LinuxFixture() => OS.Register(new LinuxOS());
}

[CollectionDefinition("Linux")]
public sealed class LinuxCollection : ICollectionFixture<LinuxFixture> { }
