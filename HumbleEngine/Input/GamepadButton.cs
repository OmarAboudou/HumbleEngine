namespace HumbleEngine;

public readonly struct GamepadButton
{
    public ButtonName Name    { get; }
    public int        Index   { get; }
    public bool       Pressed { get; }

    public GamepadButton(ButtonName name, int index, bool pressed)
    {
        Name    = name;
        Index   = index;
        Pressed = pressed;
    }
}
