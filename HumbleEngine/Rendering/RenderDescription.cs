namespace HumbleEngine;

// Description visuelle retournée par Node.Render().
// Valeur transitoire — jamais stockée dans RenderNodeTree.
// Les constructeurs sont internal : seuls les builders du moteur
// (Text, Column, Box, etc.) peuvent créer des RenderDescription.
public readonly struct RenderDescription
{
    public static readonly RenderDescription None = default;

    public static implicit operator RenderDescription(string content) => new Span(content);

    public RenderNodeKind       Kind     { get; internal init; }
    public Rect                 Bounds   { get; internal init; }
    public RenderDescription[]? Children { get; internal init; }
    public SpanData             Span     { get; internal init; }
    public BoxData              Box      { get; internal init; }
    public LayoutData           Layout   { get; internal init; }
    public ColumnData           Column   { get; internal init; }
    public RowData              Row      { get; internal init; }
    public Node?                Owner    { get; internal init; }  // Node propriétaire de cette description racine
}
