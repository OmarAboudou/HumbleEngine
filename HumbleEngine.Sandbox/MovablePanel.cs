namespace HumbleEngine.Sandbox;

/// <summary>
/// The Bloc 5 demo: a panel that grabs the keyboard focus when entering the
/// tree and moves on arrow keys (Shift = larger steps) — the focus routing in
/// action, no hit-test involved. X11 auto-repeats held keys; Wayland sends one
/// press per stroke (client-side repeat is deferred to the text field).
/// </summary>
public sealed class MovablePanel : Panel
{
    /// <inheritdoc />
    protected override void OnAttached() => GrabFocus();

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        if (inputEvent is not KeyPressed pressed)
            return false;

        var step = pressed.Modifiers.HasFlag(KeyModifiers.Shift) ? 40f : 10f;
        Vector2? delta = pressed.Key switch
        {
            Key.Left  => new Vector2(-step, 0f),
            Key.Right => new Vector2(step, 0f),
            Key.Up    => new Vector2(0f, -step),
            Key.Down  => new Vector2(0f, step),
            _         => null,
        };
        if (delta is null)
            return false;

        Position.Value += delta.Value;
        return true;
    }
}
