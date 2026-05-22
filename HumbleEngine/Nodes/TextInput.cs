using Silk.NET.Input;
using SkiaSharp;

namespace HumbleEngine;

public class TextInput : Node
{
    private const float PaddingX = 12f;
    private const float PaddingY = 6f;

    public ReactiveProperty<string> Text        = new("");
    public ReactiveProperty<string> Placeholder = new("...");
    public ReactiveProperty<float>  FontSize    = new(16f);
    public ReactiveProperty<float>  Width       = new(200f);

    private bool _isFocused;

    public override HitTestFilter MouseFilter => HitTestFilter.Stop;
    public override bool IsFocusable => true;

    public override void Init()
    {
        base.Init();
        Text.Connect(_        => MarkLayoutDirty());
        Placeholder.Connect(_ => MarkLayoutDirty());
        FontSize.Connect(_    => MarkLayoutDirty());
        Width.Connect(_       => MarkLayoutDirty());
    }

    public override void OnFocusGained() { _isFocused = true;  MarkPaintDirty(); }
    public override void OnFocusLost()   { _isFocused = false; MarkPaintDirty(); }

    public override void OnKeyChar(char c)
    {
        Text.Value += c;
    }

    public override void OnKeyDown(Key key)
    {
        if (key == Key.Backspace && Text.Value.Length > 0)
            Text.Value = Text.Value[..^1];
    }

    protected override RenderDescription RenderContent()
    {
        var borderColor = _isFocused ? new SKColor(70, 130, 180) : new SKColor(180, 180, 180);
        var textColor   = Text.Value.Length > 0 ? SKColors.Black : new SKColor(160, 160, 160);
        var displayed   = Text.Value.Length > 0 ? Text.Value : Placeholder.Value;

        return new Box
        {
            new Span(displayed).Color(textColor).FontSize(FontSize.Value)
        }.Color(SKColors.White).Border(borderColor, 1.5f).Padding(PaddingX, PaddingY).Width(Width.Value);
    }
}
