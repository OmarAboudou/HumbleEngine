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
        Text.Connect(_     => MarkLayoutDirty());
        Color.Connect(_    => MarkPaintDirty());
        FontSize.Connect(_ => MarkLayoutDirty());
    }

    protected override RenderDescription RenderContent()
        => new Span(Text.Value).Color(Color.Value).FontSize(FontSize.Value);
}
