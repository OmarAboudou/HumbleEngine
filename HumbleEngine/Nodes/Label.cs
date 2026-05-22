using SkiaSharp;

namespace HumbleEngine;

public class Label : Node
{
    public ReactiveProperty<string>  Text     = new("");
    public ReactiveProperty<SKColor> Color    = new(SKColors.Black);
    public ReactiveProperty<float>   FontSize = new(16f);

    public override void Init()
    {
        base.Init();
        Text.Connect(_ => MarkPaintDirty());
        Color.Connect(_ => MarkPaintDirty());
        FontSize.Connect(_ => MarkLayoutDirty());
    }

    public override void Layout(Size available)
    {
        using var font = new SKFont(SKTypeface.Default, FontSize.Value);
        var width  = font.MeasureText(Text.Value);
        var height = font.Metrics.Descent - font.Metrics.Ascent;

        ComputedBounds = ComputedBounds with { Width = width, Height = height };
    }

    protected override RenderDescription RenderContent()
        => new Span(Text.Value).Color(Color.Value).FontSize(FontSize.Value);
}
