using System.Runtime.CompilerServices;

namespace HumbleEngine.Linux;

// S'exécute automatiquement au chargement de l'assembly — enregistre LinuxOS
// sans que l'application ait à appeler quoi que ce soit explicitement.
internal static class LinuxModuleInit
{
    [ModuleInitializer]
    internal static void Initialize() => OS.Register(new LinuxOS());
}
