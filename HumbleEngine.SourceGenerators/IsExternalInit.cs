// Shim requis pour utiliser "record" et "init" quand on cible netstandard2.0
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
