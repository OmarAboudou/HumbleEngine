using SkiaSharp;

namespace HumbleEngine;

// Builder pour un nœud de rendu texte.
// Usage : new Text("Bonjour") ou new Text("Bonjour", SKColors.Red, fontSize: 24f)
public readonly struct Text
{
    private readonly string  _content;
    private readonly SKColor _color;
    private readonly float   _fontSize;

    public Text(string content, SKColor color = default, float fontSize = 16f)
    {
        _content  = content;
        _color    = color == default ? SKColors.Black : color;
        _fontSize = fontSize;
    }

    public static implicit operator RenderDescription(Text t) => new()
    {
        Kind = RenderNodeKind.Text,
        Text = new TextData { Content = t._content, Color = t._color, FontSize = t._fontSize }
    };
}
