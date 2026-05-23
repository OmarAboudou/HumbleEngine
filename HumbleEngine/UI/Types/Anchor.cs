namespace HumbleEngine;

// Positionnement normalisé 0-1 dans le parent (modèle Godot)
// Left=0 → bord gauche du parent, Left=1 → bord droit du parent
public readonly struct Anchor : IEquatable<Anchor>
{
    public float Left   { get; init; }
    public float Top    { get; init; }
    public float Right  { get; init; }
    public float Bottom { get; init; }

    public static readonly Anchor TopLeft      = new() { Left = 0,    Top = 0,    Right = 0,    Bottom = 0    };
    public static readonly Anchor TopRight     = new() { Left = 1,    Top = 0,    Right = 1,    Bottom = 0    };
    public static readonly Anchor BottomLeft   = new() { Left = 0,    Top = 1,    Right = 0,    Bottom = 1    };
    public static readonly Anchor BottomRight  = new() { Left = 1,    Top = 1,    Right = 1,    Bottom = 1    };
    public static readonly Anchor Center       = new() { Left = .5f,  Top = .5f,  Right = .5f,  Bottom = .5f  };
    public static readonly Anchor FullStretch  = new() { Left = 0,    Top = 0,    Right = 1,    Bottom = 1    };
    public static readonly Anchor TopStretch   = new() { Left = 0,    Top = 0,    Right = 1,    Bottom = 0    };
    public static readonly Anchor BottomStretch= new() { Left = 0,    Top = 1,    Right = 1,    Bottom = 1    };
    public static readonly Anchor LeftStretch  = new() { Left = 0,    Top = 0,    Right = 0,    Bottom = 1    };
    public static readonly Anchor RightStretch = new() { Left = 1,    Top = 0,    Right = 1,    Bottom = 1    };

    public bool Equals(Anchor other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    public override bool Equals(object? obj) => obj is Anchor a && Equals(a);
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
    public static bool operator ==(Anchor a, Anchor b) => a.Equals(b);
    public static bool operator !=(Anchor a, Anchor b) => !a.Equals(b);
}
