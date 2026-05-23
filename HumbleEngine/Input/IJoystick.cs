using System.Collections.Generic;

namespace HumbleEngine;

public interface IJoystick : IInputDevice
{
    IReadOnlyList<Axis>          Axes     { get; }
    IReadOnlyList<GamepadButton> Buttons  { get; }
    IReadOnlyList<Hat>           Hats     { get; }
    Deadzone                     Deadzone { get; set; }

    IReadOnlySignal<GamepadButton> OnButtonDown { get; }
    IReadOnlySignal<GamepadButton> OnButtonUp   { get; }
    IReadOnlySignal<Axis>          OnAxisMoved  { get; }
    IReadOnlySignal<Hat>           OnHatMoved   { get; }
}
