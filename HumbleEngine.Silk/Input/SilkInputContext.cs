using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkCtx = Silk.NET.Input.IInputContext;

namespace HumbleEngine.Silk;

public class SilkInputContext : IInputContext
{
    private readonly Signal<IInputDevice, bool> _onConnectionChanged = new();

    public SilkInputContext(SilkCtx ctx)
    {
        Handle       = ctx.Handle;
        Keyboards    = ctx.Keyboards.Select(k => (IKeyboard)new SilkKeyboard(k)).ToList();
        Mice         = ctx.Mice.Select(m => (IMouse)new SilkMouse(m)).ToList();
        Gamepads     = ctx.Gamepads.Select(g => (IGamepad)new SilkGamepad(g)).ToList();
        Joysticks    = ctx.Joysticks.Select(j => (IJoystick)new SilkJoystick(j)).ToList();
        OtherDevices = ctx.OtherDevices.Select(d => (IInputDevice)new SilkInputDevice(d)).ToList();

        ctx.ConnectionChanged += (device, connected) =>
            _onConnectionChanged.Emit(new SilkInputDevice(device), connected);
    }

    public nint                        Handle       { get; }
    public IReadOnlyList<IKeyboard>    Keyboards    { get; }
    public IReadOnlyList<IMouse>       Mice         { get; }
    public IReadOnlyList<IGamepad>     Gamepads     { get; }
    public IReadOnlyList<IJoystick>    Joysticks    { get; }
    public IReadOnlyList<IInputDevice> OtherDevices { get; }

    public IReadOnlySignal<IInputDevice, bool> OnConnectionChanged => _onConnectionChanged.AsReadOnly();
}
