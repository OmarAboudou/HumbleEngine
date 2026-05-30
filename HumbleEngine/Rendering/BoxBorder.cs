namespace HumbleEngine;

public readonly record struct BoxBorder(BorderSide Top, BorderSide Right, BorderSide Bottom, BorderSide Left)
{
    public static BoxBorder All(BorderSide side)          => new(side, side, side, side);
    public static BoxBorder All(Color color, float width) => All(new BorderSide(color, width));
    public static BoxBorder Symmetric(
        BorderSide vertical   = default,
        BorderSide horizontal = default)
        => new(vertical, horizontal, vertical, horizontal);

    public bool IsUniform =>
        Top == Right && Right == Bottom && Bottom == Left;
}
