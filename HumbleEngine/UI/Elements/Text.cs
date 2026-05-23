namespace HumbleEngine;

public record Text(string Content) : RenderElement
{
    public float        FontSize { get; init; } = 16f;
    public Color        Color    { get; init; } = Color.Black;
    public TextAlign    Align    { get; init; }
    public TextOverflow Overflow { get; init; }
    public ITypeface?   Typeface { get; init; }
}

public static class TextExtensions
{
    public static Text Color(this Text el, Color color)       => el with { Color    = color  };
    public static Text FontSize(this Text el, float size)     => el with { FontSize = size   };
    public static Text Align(this Text el, TextAlign align)   => el with { Align    = align  };
    public static Text Overflow(this Text el, TextOverflow v) => el with { Overflow = v      };
    public static Text Typeface(this Text el, ITypeface face) => el with { Typeface = face   };
}
