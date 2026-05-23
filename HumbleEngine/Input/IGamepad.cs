using System.Collections.Generic;

namespace HumbleEngine;

public interface IGamepad : IInputDevice
{
    IReadOnlyList<GamepadButton> Buttons         { get; }
    IReadOnlyList<Thumbstick>    Thumbsticks      { get; }
    IReadOnlyList<Trigger>       Triggers         { get; }
    IReadOnlyList<IMotor>        VibrationMotors  { get; }
    Deadzone                     Deadzone         { get; set; }

    IReadOnlySignal<GamepadButton> OnButtonDown      { get; }
    IReadOnlySignal<GamepadButton> OnButtonUp        { get; }
    IReadOnlySignal<Thumbstick>    OnThumbstickMoved { get; }
    IReadOnlySignal<Trigger>       OnTriggerMoved    { get; }
}
