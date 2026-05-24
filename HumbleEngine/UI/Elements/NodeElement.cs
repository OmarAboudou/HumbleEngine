namespace HumbleEngine;

/// <summary>
/// Couture dans l'arbre RenderElement : délègue layout et rendu à un UINode embarqué.
/// Le LayoutEngine résout Node.GetElement() à chaque frame — le cache interne du UINode
/// évite de recalculer quand il n'est pas dirty.
/// </summary>
public sealed record NodeElement(UINode Node) : RenderElement;
