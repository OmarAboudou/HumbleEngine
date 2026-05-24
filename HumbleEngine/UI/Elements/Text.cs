namespace HumbleEngine;

public record Text : RenderElement
{
    public Text(string content, Action? onMouseEnter = null, Action? onMouseExit = null, Action? onClick = null)
        : this(new Property<string>(content))
    {
        OnMouseEnter = onMouseEnter;
        OnMouseExit  = onMouseExit;
        OnClick      = onClick;
    }
    public Text(Property<string> content) { ContentProperty = content; }

    private Property<string> ContentProperty { get; init; } = default!;
    public string Content => ContentProperty.Value;

    public Font         Font         { get; init; } = Font.Default;
    public Color        Color        { get; init; } = Color.Black;
    public TextAlign    Align        { get; init; }
    public TextOverflow TextOverflow { get; init; }
}

public static class TextExtensions
{
    public static Text Color(this Text el, Color color)       => el with { Color        = color };
    public static Text Font(this Text el, Font font)          => el with { Font         = font  };
    public static Text Align(this Text el, TextAlign align)   => el with { Align        = align };
    public static Text Overflow(this Text el, TextOverflow v) => el with { TextOverflow = v     };
}
