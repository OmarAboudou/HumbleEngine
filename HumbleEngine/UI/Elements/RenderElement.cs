namespace HumbleEngine;

public abstract record RenderElement
{
    public object? Key    { get; init; }

    public Length  Width     { get; init; }
    public Length  Height    { get; init; }
    public Length  MinWidth  { get; init; }
    public Length  MaxWidth  { get; init; }
    public Length  MinHeight { get; init; }
    public Length  MaxHeight { get; init; }

    public EdgeInsets Padding { get; init; }
    public EdgeInsets Margin  { get; init; }

    public float        Opacity      { get; init; } = 1f;
    public Color        Background   { get; init; }
    public CornerRadius CornerRadius { get; init; }
    public Overflow     Overflow     { get; init; }

    public Color BorderColor { get; init; }
    public float BorderWidth { get; init; }

    public Matrix Transform { get; init; } = Matrix.Identity;

    /// <summary>
    /// Positionnement normalisé dans le parent (0 = bord gauche/haut, 1 = bord droit/bas).
    /// Ignoré hors d'un <c>Canvas</c> ou <c>Stack</c> — sans effet dans un layout de flux (VLayout, HLayout).
    /// </summary>
    public Anchor? Anchor { get; init; }
}

public static class RenderElementExtensions
{
    public static T Key<T>(this T el, object key)             where T : RenderElement => el with { Key          = key      };

    public static T Width<T>(this T el, Length v)             where T : RenderElement => el with { Width        = v        };
    public static T Height<T>(this T el, Length v)            where T : RenderElement => el with { Height       = v        };
    public static T Size<T>(this T el, Length w, Length h)    where T : RenderElement => el with { Width        = w, Height = h };
    public static T MinWidth<T>(this T el, Length v)          where T : RenderElement => el with { MinWidth     = v        };
    public static T MaxWidth<T>(this T el, Length v)          where T : RenderElement => el with { MaxWidth     = v        };
    public static T MinHeight<T>(this T el, Length v)         where T : RenderElement => el with { MinHeight    = v        };
    public static T MaxHeight<T>(this T el, Length v)         where T : RenderElement => el with { MaxHeight    = v        };

    public static T Padding<T>(this T el, EdgeInsets v)       where T : RenderElement => el with { Padding      = v        };
    public static T Margin<T>(this T el, EdgeInsets v)        where T : RenderElement => el with { Margin       = v        };

    public static T Opacity<T>(this T el, float v)            where T : RenderElement => el with { Opacity      = v        };
    public static T Background<T>(this T el, Color color)     where T : RenderElement => el with { Background   = color    };
    public static T CornerRadius<T>(this T el, CornerRadius r) where T : RenderElement => el with { CornerRadius = r        };
    public static T Overflow<T>(this T el, Overflow overflow)   where T : RenderElement => el with { Overflow     = overflow  };

    public static T BorderColor<T>(this T el, Color color)     where T : RenderElement => el with { BorderColor  = color     };
    public static T BorderWidth<T>(this T el, float width)     where T : RenderElement => el with { BorderWidth  = width     };
    public static T Border<T>(this T el, Color color, float width) where T : RenderElement => el with { BorderColor = color, BorderWidth = width };

    /// <inheritdoc cref="RenderElement.Anchor"/>
    public static T Anchor<T>(this T el, Anchor anchor)       where T : RenderElement => el with { Anchor       = anchor   };

    public static T Translate<T>(this T el, float x, float y) where T : RenderElement => el with { Transform = el.Transform * Matrix.CreateTranslation(x, y)     };
    public static T Scale<T>(this T el, float sx, float sy)   where T : RenderElement => el with { Transform = el.Transform * Matrix.CreateScale(sx, sy)          };
    public static T Rotate<T>(this T el, float degrees)       where T : RenderElement => el with { Transform = el.Transform * Matrix.CreateRotationDegrees(degrees) };
}
