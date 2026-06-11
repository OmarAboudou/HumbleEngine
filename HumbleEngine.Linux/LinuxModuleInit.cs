using System.Runtime.CompilerServices;

namespace HumbleEngine.Linux;

/// <summary>
/// Runs automatically when the assembly is loaded and registers <see cref="LinuxOS"/>
/// so the application does not need to call anything explicitly.
/// </summary>
internal static class LinuxModuleInit
{
    [ModuleInitializer]
    internal static void Initialize() => OS.Register(new LinuxOS());
}
