namespace HumbleEngine;

/// <summary>Mobile surface backend: provides the single surface imposed by the OS.</summary>
public interface IMobileSurfaceBackend : ISurfaceBackend
{
    /// <summary>Returns the mobile surface associated with the current activity or view.</summary>
    IMobileSurface GetSurface(MobileSurfaceDescription description);
}
