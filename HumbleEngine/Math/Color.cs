namespace HumbleEngine;

public readonly struct Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);
    public static Color FromRgb(byte r, byte g, byte b)           => new(r, g, b);

    public uint ToArgb() => (uint)((A << 24) | (R << 16) | (G << 8) | B);

    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color Black       = new(0, 0, 0);
    public static readonly Color White       = new(255, 255, 255);
    public static readonly Color Red         = new(255, 0, 0);
    public static readonly Color Green       = new(0, 255, 0);
    public static readonly Color Blue        = new(0, 0, 255);

    public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override bool Equals(object? obj) => obj is Color c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public override string ToString() => $"rgba({R},{G},{B},{A})";
}
