namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Représentation structurée et récursive d'une référence à un type C#,
/// utilisée dans les <c>generic_bindings</c> des nodes et EmbeddedScenes.
///
/// <para>
/// Un <see cref="TypeRef"/> peut représenter aussi bien un type simple
/// (<c>Game.Sword</c>) qu'un type générique fermé arbitrairement profond
/// (<c>Inventory&lt;List&lt;Sword&gt;&gt;</c>). La récursivité est portée
/// par <see cref="Args"/> — chaque argument générique est lui-même un <see cref="TypeRef"/>.
/// </para>
///
/// <para>
/// Exemples de représentations JSON équivalentes :
/// <code>
/// // Type simple — chaîne directe acceptée par le parser
/// "TDamage": "Game.SlashDamage"
///
/// // Type générique fermé — objet structuré
/// "TContainer": { "type": "Game.Inventory`1", "args": ["Game.Sword"] }
///
/// // Générique profond : Inventory&lt;List&lt;Sword&gt;&gt;
/// "TContainer": {
///   "type": "Game.Inventory`1",
///   "args": [{ "type": "System.Collections.Generic.List`1", "args": ["Game.Sword"] }]
/// }
/// </code>
/// </para>
/// </summary>
/// <param name="TypeName">
/// Nom qualifié du type C# en notation réflexion.
/// Pour les types génériques, inclut le suffixe d'arité : <c>Game.Inventory`1</c>,
/// <c>System.Collections.Generic.Dictionary`2</c>, etc.
/// </param>
/// <param name="Args">
/// Arguments génériques de ce type, dans l'ordre de déclaration.
/// Vide pour un type non générique ou un type générique ouvert.
/// Chaque argument est lui-même un <see cref="TypeRef"/> — la structure est récursive.
/// </param>
public sealed record TypeRef(string TypeName, IReadOnlyList<TypeRef> Args)
{
    /// <summary>
    /// Crée un <see cref="TypeRef"/> simple sans arguments génériques.
    /// Raccourci pour le cas courant d'un type non générique.
    /// </summary>
    public static TypeRef Simple(string typeName) => new(typeName, Array.Empty<TypeRef>());

    /// <summary>
    /// Indique si ce type a des arguments génériques.
    /// </summary>
    public bool IsGeneric => Args.Count > 0;

    public bool Equals(TypeRef? other) =>
        other is not null &&
        TypeName == other.TypeName &&
        Args.SequenceEqual(other.Args);
    
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TypeName);
        foreach (var arg in Args)
            hash.Add(arg);
        return hash.ToHashCode();
    }
    
    /// <summary>
    /// Retourne une représentation lisible du type, analogue à la syntaxe C#.
    /// Utile pour les messages de diagnostic et le débogage.
    /// Exemple : <c>Game.Inventory&lt;Game.Sword&gt;</c>
    /// </summary>
    public override string ToString() => IsGeneric
        ? $"{TypeName}<{string.Join(", ", Args)}>"
        : TypeName;
}