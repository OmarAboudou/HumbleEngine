using System.Collections.Generic;

namespace HumbleEngine;

public interface IInputContext
{
    nint                        Handle       { get; }
    IReadOnlyList<IKeyboard>    Keyboards    { get; }
    IReadOnlyList<IMouse>       Mice         { get; }
    IReadOnlyList<IGamepad>     Gamepads     { get; }
    IReadOnlyList<IJoystick>    Joysticks    { get; }
    IReadOnlyList<IInputDevice> OtherDevices { get; }

    IReadOnlySignal<IInputDevice, bool> OnConnectionChanged { get; }
}
