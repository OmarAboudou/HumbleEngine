namespace HumbleEngine;

public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Black       => new(0, 0, 0);
    public static Color White       => new(255, 255, 255);
    public static Color Red         => new(255, 0, 0);
    public static Color Green       => new(0, 255, 0);
    public static Color Blue        => new(0, 0, 255);

    public static Color FromARGB(byte a, byte r, byte g, byte b) => new(r, g, b, a);
    public Color WithAlpha(byte alpha) => this with { A = alpha };
}
