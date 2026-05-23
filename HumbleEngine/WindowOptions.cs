namespace HumbleEngine;

public record WindowOptions(string Title, Vector2<int> Size)
{
    public Vector2<int>? Position     { get; init; }
    public WindowState   WindowState  { get; init; } = WindowState.Normal;
    public WindowBorder  WindowBorder { get; init; } = WindowBorder.Resizable;
    public bool          IsVisible    { get; init; } = true;
    public bool          TopMost      { get; init; } = false;

    public static WindowOptions Default => new("HumbleEngine", new Vector2<int>(1280, 720));
}
