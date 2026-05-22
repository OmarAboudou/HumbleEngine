using SkiaSharp;

namespace HumbleEngine;

public class Button : Node
{
    private const float PaddingX = 16f;
    private const float PaddingY = 8f;

    public ReactiveProperty<string>  Text            = new("");
    public ReactiveProperty<SKColor> BackgroundColor = new(new SKColor(220, 220, 220));
    public ReactiveProperty<SKColor> HoverColor      = new(new SKColor(190, 210, 240));
    public ReactiveProperty<float>   FontSize        = new(16f);

    private bool _isHovered;

    private readonly MutableSignal _pressed = new();
    public Signal Pressed => _pressed.Signal;

    public override HitTestFilter MouseFilter => HitTestFilter.Stop;

    public override void Init()
    {
        base.Init();
        Text.Connect(_  => MarkLayoutDirty());
        FontSize.Connect(_ => MarkLayoutDirty());
        BackgroundColor.Connect(_ => MarkPaintDirty());
        HoverColor.Connect(_ => MarkPaintDirty());
    }

    public override void OnMouseEnter() { _isHovered = true;  MarkPaintDirty(); }
    public override void OnMouseLeave() { _isHovered = false; MarkPaintDirty(); }
    public override void OnClick()      { _pressed.Emit(); }

    protected override RenderDescription RenderContent()
    {
        var bg = _isHovered ? HoverColor.Value : BackgroundColor.Value;
        return new Box
        {
            new Span(Text.Value).Color(SKColors.Black).FontSize(FontSize.Value)
        }.Color(bg).Padding(PaddingX, PaddingY);
    }
}
