namespace HumbleEngine;

// TextBlock requires a parent with a constrained width (explicit Px, Fill, or Percent).
// Inside a fit-content parent, TextBlock fills the available width and prevents shrinking.
public record TextBlock : RenderElement
{
    public TextBlock(string content, Action? onMouseEnter = null, Action? onMouseExit = null, Action? onClick = null)
        : this(new Property<string>(content))
    {
        OnMouseEnter = onMouseEnter;
        OnMouseExit  = onMouseExit;
        OnClick      = onClick;
    }
    public TextBlock(Property<string> content) { ContentProperty = content; }

    private Property<string> ContentProperty { get; init; } = default!;
    public string Content => ContentProperty.Value;

    public Font         Font         { get; init; } = Font.Default;
    public Color        Color        { get; init; } = Color.Black;
    public TextAlign    Align        { get; init; }
    public TextOverflow TextOverflow { get; init; }
}

public static class TextBlockExtensions
{
    public static TextBlock Color(this TextBlock el, Color color)       => el with { Color        = color };
    public static TextBlock Font(this TextBlock el, Font font)          => el with { Font         = font  };
    public static TextBlock Align(this TextBlock el, TextAlign align)   => el with { Align        = align };
    public static TextBlock Overflow(this TextBlock el, TextOverflow v) => el with { TextOverflow = v     };
}
