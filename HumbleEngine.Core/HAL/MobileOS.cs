namespace HumbleEngine;

public abstract class MobileOS : OS
{
    // Sur mobile, l'OS impose une surface unique — pas de liste, pas de choix.
    public abstract IMobileSurfaceBackend SurfaceBackend { get; }
}
