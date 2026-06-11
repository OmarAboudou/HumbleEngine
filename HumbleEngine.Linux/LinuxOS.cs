namespace HumbleEngine.Linux;

public class LinuxOS : OS
{
    public override string Name => "Linux";
    public override IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; } = [];
    public override IReadOnlyList<IGraphicSurfaceBackend> AvailableGraphicsSurfaceBackends { get; } = [];

}