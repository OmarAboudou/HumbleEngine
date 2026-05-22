using SkiaSharp;

namespace HumbleEngine;

public struct Span : IRenderNode
{
    private readonly string  _content;
    private readonly SKColor _color;
    private readonly float   _fontSize;
    private LayoutData       _layout;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public Span(string content, SKColor color = default, float fontSize = 16f)
    {
        _content  = content;
        _color    = color == default ? SKColors.Black : color;
        _fontSize = fontSize;
    }

    public static implicit operator RenderDescription(Span s) => new()
    {
        Kind   = RenderNodeKind.Span,
        Span   = new SpanData { Content = s._content, Color = s._color, FontSize = s._fontSize },
        Layout = s._layout
    };

    public RenderDescription At(Rect bounds) => ((RenderDescription)this) with { Bounds = bounds };

    public RenderDescription At(float x, float y) => At(new Rect(x, y, 0, 0));
}
