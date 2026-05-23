namespace HumbleEngine;

public readonly struct Length : IEquatable<Length>
{
    public float      Value { get; }
    public LengthKind Kind  { get; }

    private Length(float value, LengthKind kind) { Value = value; Kind = kind; }

    public static Length Px(float value)      => new(value, LengthKind.Px);
    public static Length Percent(float value) => new(value, LengthKind.Percent);
    public static Length Vw(float value)      => new(value, LengthKind.Vw);
    public static Length Vh(float value)      => new(value, LengthKind.Vh);

    public static readonly Length Auto = new(0, LengthKind.Auto);
    public static readonly Length Fill = new(0, LengthKind.Fill);
    public static readonly Length Zero = Px(0);

    public static implicit operator Length(float px) => Px(px);

    public bool Equals(Length other) => Kind == other.Kind && Value == other.Value;
    public override bool Equals(object? obj) => obj is Length l && Equals(l);
    public override int GetHashCode() => HashCode.Combine(Kind, Value);
    public static bool operator ==(Length a, Length b) => a.Equals(b);
    public static bool operator !=(Length a, Length b) => !a.Equals(b);

    public override string ToString() => Kind switch
    {
        LengthKind.Px      => $"{Value}px",
        LengthKind.Percent => $"{Value}%",
        LengthKind.Vw      => $"{Value}vw",
        LengthKind.Vh      => $"{Value}vh",
        LengthKind.Auto    => "auto",
        LengthKind.Fill    => "fill",
        _                  => "auto",
    };
}

public enum LengthKind { Auto, Px, Percent, Vw, Vh, Fill }

public static class LengthExtensions
{
    public static Length Px(this float v)      => Length.Px(v);
    public static Length Percent(this float v) => Length.Percent(v);
    public static Length Vw(this float v)      => Length.Vw(v);
    public static Length Vh(this float v)      => Length.Vh(v);

    public static Length Px(this int v)        => Length.Px(v);
    public static Length Percent(this int v)   => Length.Percent(v);
    public static Length Vw(this int v)        => Length.Vw(v);
    public static Length Vh(this int v)        => Length.Vh(v);
}
