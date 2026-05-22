namespace HumbleEngine;

public readonly struct RenderDescription
{
    public static readonly RenderDescription None = default;

    public static implicit operator RenderDescription(string content) => new Span(content);

    public RenderNodeKind       Kind     { get; internal init; }
    public Rect                 Bounds   { get; internal init; }
    public RenderDescription[]? Children { get; internal init; }
    public SpanData             Span     { get; internal init; }
    public BoxData              Box      { get; internal init; }
    public VLayoutData          VLayout  { get; internal init; }
    public HLayoutData          HLayout  { get; internal init; }
    public LayoutData           Layout   { get; internal init; }
    public Node?                Owner    { get; internal init; }
}
