namespace HumbleEngine.Core;

/// <summary>
/// Marque une propriété NodeSlot&lt;T&gt; comme un point d'insertion nommé
/// et typé, injectable depuis une scène englobante ou une scène héritière.
///
/// L'attribut est posé sur la propriété NodeSlot&lt;T&gt;, pas sur le champ
/// node cible interne.
/// </summary>
/// <example>
/// <code>
/// public class InventoryNode : Node
/// {
///     private GridNode _grid;
///
///     [Slot(Description = "Éléments de l'inventaire.")]
///     public NodeSlot&lt;InventoryEntry&gt; Entries => GetSlot&lt;InventoryEntry&gt;(_grid);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SlotAttribute : Attribute
{
    /// <summary>
    /// Description optionnelle du slot, affichée dans l'éditeur.
    /// </summary>
    public string? Description { get; init; }
}