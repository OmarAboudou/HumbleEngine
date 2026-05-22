namespace HumbleEngine;

// Valeur transitoire retournée par Node.CreateRenderNode().
// N'est jamais stockée dans le RenderNodeTree — sert uniquement
// à transporter les données du Node vers le Build.
public readonly struct RenderNodeData
{
    public static readonly RenderNodeData None = default;

    public RenderNodeKind Kind { get; init; }
    public TextData       Text { get; init; }
    // Ajouter les autres types ici au fur et à mesure (BoxData, ImageData, etc.)
}
