namespace HumbleEngine.Tests.Infrastructure;

/// <summary>
/// Minimal DesktopOS with no system resources. Used to test Core OS contracts
/// without depending on any real platform assembly.
/// </summary>
internal sealed class FakeDesktopOS : DesktopOS
{
    public override string Name => "Fake";
    public override IReadOnlyList<IWindowBackend>   AvailableWindowBackends   => [];
    public override IWindowBackend                  DefaultWindowBackend      => throw new NotSupportedException();
    public override IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends => [];
    public override IGraphicsBackend                DefaultGraphicsBackend    => throw new NotSupportedException();
}
