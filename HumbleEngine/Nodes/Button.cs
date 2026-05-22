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

    public override void Layout(Size available)
    {
        using var font  = new SKFont(SKTypeface.Default, FontSize.Value);
        var textWidth   = font.MeasureText(Text.Value);
        var textHeight  = font.Metrics.Descent - font.Metrics.Ascent;

        ComputedBounds = ComputedBounds with
        {
            Width  = textWidth  + PaddingX * 2,
            Height = textHeight + PaddingY * 2,
        };
    }

    protected override RenderDescription RenderContent()
    {
        var bg = _isHovered ? HoverColor.Value : BackgroundColor.Value;
        return new Box(bg)
        {
            new Span(Text.Value, SKColors.Black, FontSize.Value).At(PaddingX, PaddingY)
        };
    }
}
