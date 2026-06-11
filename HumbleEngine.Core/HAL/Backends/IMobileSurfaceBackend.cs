namespace HumbleEngine;

public interface IMobileSurfaceBackend : ISurfaceBackend
{
    IMobileSurface GetSurface(MobileSurfaceDescription description);
}
