namespace HumbleEngine.Sandbox;

/// <summary>
/// Interactive panel of the Bloc 4 demo: brightens under the pointer (the
/// router's synthesized hover), and a left click queues its disposal — the
/// whole engine in one click: hit-test, bubbling, QueueDispose honoured at
/// the end-of-frame flush, and the Column restacking on the departure
/// narration.
/// </summary>
public sealed class Tile : Panel
{
    private readonly Vector4 _baseColor;

    public Tile(Vector4 color)
    {
        _baseColor  = color;
        Color.Value = color;
    }

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case PointerEntered:
                Color.Value = Lighten(_baseColor);
                return false; // hover feedback consumes nothing

            case PointerExited:
                Color.Value = _baseColor;
                return false;

            case PointerPressed { Button: PointerButton.Left }:
                Console.WriteLine($"[ui] {this} clicked — queueing disposal.");
                QueueDispose();
                return true;

            default:
                return false;
        }
    }

    /// <summary>Pushes the colour a third of the way towards white.</summary>
    private static Vector4 Lighten(Vector4 color) => new(
        color.X + (1f - color.X) / 3f,
        color.Y + (1f - color.Y) / 3f,
        color.Z + (1f - color.Z) / 3f,
        color.W);
}
