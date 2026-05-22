namespace HumbleEngine;

// Description visuelle retournée par Node.Render().
// Valeur transitoire — jamais stockée dans RenderNodeTree.
// Les constructeurs sont internal : seuls les builders du moteur
// (Text, Column, Box, etc.) peuvent créer des RenderDescription.
public readonly struct RenderDescription
{
    public static readonly RenderDescription None = default;

    public RenderNodeKind       Kind     { get; internal init; }
    public Rect                 Bounds   { get; internal init; }  // injecté par Node.Render()
    public RenderDescription[]? Children { get; internal init; }  // pour les conteneurs
    public TextData             Text     { get; internal init; }
    public BoxData              Box      { get; internal init; }
}
