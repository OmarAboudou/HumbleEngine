namespace HumbleEngine;

// Nœud de rendu stocké dans le tableau plat du RenderNodeTree.
// Taille fixe quelle que soit la nature du nœud.
// Les données spécifiques au type sont dans les tableaux typés du RenderNodeTree,
// accessibles via Index.
public readonly struct RenderNode
{
    public RenderNodeKind Kind   { get; init; }  // quel type de nœud
    public int            Index  { get; init; }  // index dans le tableau typé correspondant
    public Rect           Bounds { get; init; }  // bounds absolues (calculées pendant Rebuild)
}
