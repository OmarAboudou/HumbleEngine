namespace HumbleEngine;

/// <summary>
/// Mobile platform descriptor.
/// On mobile the OS imposes a single surface — there is no list of backends to choose from.
/// </summary>
public abstract class MobileOS : OS
{
    /// <summary>The single surface backend imposed by the mobile OS.</summary>
    public abstract IMobileSurfaceBackend SurfaceBackend { get; }
}
