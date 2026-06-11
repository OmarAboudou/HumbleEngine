namespace HumbleEngine;

public abstract class OS
{
    public static OS Current { get; }

    public abstract string Name { get; }
    public abstract IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }
    public abstract IReadOnlyList<IGraphicSurfaceBackend> AvailableGraphicsSurfaceBackends { get; }
    public IGraphicsBackend DefaultGraphicsBackend => AvailableGraphicsBackends[0];
    public IGraphicSurfaceBackend DefaultGraphicSurfaceBackend => AvailableGraphicsSurfaceBackends[0];

}