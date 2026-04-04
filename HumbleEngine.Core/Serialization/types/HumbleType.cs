namespace HumbleEngine.Core;

/// <summary>
/// Identifiant stable d'un type C# enregistré dans le <see cref="HumbleTypeRegistry"/>.
/// Peut être sérialisé et utilisé pour retrouver le <see cref="System.Type"/> correspondant à l'exécution.
/// </summary>
public readonly record struct HumbleType(Guid Id)
{
    /// <summary>
    /// Résout cet identifiant en son <see cref="System.Type"/> correspondant.
    /// </summary>
    /// <returns>Le <see cref="System.Type"/> associé à cet identifiant.</returns>
    /// <exception cref="ArgumentException">Levée si aucun type n'est enregistré pour <see cref="Id"/>.</exception>
    public Type Resolve() => Services.HumbleTypeRegistry.Resolve(Id);
}