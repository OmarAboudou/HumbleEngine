using SkiaSharp;

namespace HumbleEngine;

public struct Span : IRenderNode
{
    private readonly string _content;
    private SKColor         _color;
    private float           _fontSize;
    private LayoutData      _layout;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public Span(string content)
    {
        _content  = content;
        _color    = SKColors.Black;
        _fontSize = 16f;
    }

    public Span Color(SKColor color) { _color    = color; return this; }
    public Span FontSize(float size) { _fontSize = size;  return this; }

    public static implicit operator RenderDescription(Span s) => new()
    {
        Kind   = RenderNodeKind.Span,
        Span   = new SpanData { Content = s._content, Color = s._color, FontSize = s._fontSize },
        Layout = s._layout
    };

    public RenderDescription At(Rect bounds) => ((RenderDescription)this) with { Bounds = bounds };
    public RenderDescription At(float x, float y) => At(new Rect(x, y, 0, 0));
}
