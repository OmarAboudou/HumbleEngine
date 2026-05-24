namespace HumbleEngine;

/// <summary>
/// Nœud du layout tree : un élément associé à sa boîte calculée et à ses enfants positionnés.
/// </summary>
public sealed record LayoutNode(RenderElement Element, LayoutBox Box, IReadOnlyList<LayoutNode> Children)
{
    public ElementId Id { get; init; }
}
