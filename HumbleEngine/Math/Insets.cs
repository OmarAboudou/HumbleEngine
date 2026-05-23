namespace HumbleEngine;

public readonly struct Insets(int left, int top, int right, int bottom)
{
    public int Left   { get; } = left;
    public int Top    { get; } = top;
    public int Right  { get; } = right;
    public int Bottom { get; } = bottom;
}
