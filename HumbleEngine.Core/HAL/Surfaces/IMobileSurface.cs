namespace HumbleEngine;

public interface IMobileSurface : IGraphicsSurface
{
    event Action? OnPause;
    event Action? OnResume;
    event Action? OnLowMemory;
}
