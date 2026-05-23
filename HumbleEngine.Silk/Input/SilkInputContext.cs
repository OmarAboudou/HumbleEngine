using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkCtx  = Silk.NET.Input.IInputContext;
using SilkIKb  = Silk.NET.Input.IKeyboard;
using SilkIMs  = Silk.NET.Input.IMouse;
using SilkIGp  = Silk.NET.Input.IGamepad;
using SilkIJs  = Silk.NET.Input.IJoystick;
using SilkIDev = Silk.NET.Input.IInputDevice;

namespace HumbleEngine.Silk;

public class SilkInputContext : IInputContext
{
    private readonly SilkCtx _ctx;
    private readonly Signal<IInputDevice, bool> _onConnectionChanged = new();

    private readonly Dictionary<SilkIKb,  SilkKeyboard>   _keyboards = new();
    private readonly Dictionary<SilkIMs,  SilkMouse>      _mice      = new();
    private readonly Dictionary<SilkIGp,  SilkGamepad>    _gamepads  = new();
    private readonly Dictionary<SilkIJs,  SilkJoystick>   _joysticks = new();
    private readonly Dictionary<SilkIDev, SilkInputDevice> _others   = new();

    public SilkInputContext(SilkCtx ctx)
    {
        _ctx   = ctx;
        Handle = ctx.Handle;
        ctx.ConnectionChanged += (device, connected) =>
        {
            var wrapper = Wrap(device);
            _onConnectionChanged.Emit(wrapper, connected);
            if (!connected) Purge(device);
        };
    }

    public nint Handle { get; }

    public IReadOnlyList<IKeyboard>    Keyboards    => _ctx.Keyboards.Select(Wrap).ToList();
    public IReadOnlyList<IMouse>       Mice         => _ctx.Mice.Select(Wrap).ToList();
    public IReadOnlyList<IGamepad>     Gamepads     => _ctx.Gamepads.Select(Wrap).ToList();
    public IReadOnlyList<IJoystick>    Joysticks    => _ctx.Joysticks.Select(Wrap).ToList();
    public IReadOnlyList<IInputDevice> OtherDevices => _ctx.OtherDevices.Select(Wrap).ToList();

    public IReadOnlySignal<IInputDevice, bool> OnConnectionChanged => _onConnectionChanged.AsReadOnly();

    private SilkKeyboard    Wrap(SilkIKb  k) => _keyboards.TryGetValue(k, out var w) ? w : _keyboards[k]  = new SilkKeyboard(k);
    private SilkMouse       Wrap(SilkIMs  m) => _mice.TryGetValue(m,      out var w) ? w : _mice[m]       = new SilkMouse(m);
    private SilkGamepad     Wrap(SilkIGp  g) => _gamepads.TryGetValue(g,  out var w) ? w : _gamepads[g]   = new SilkGamepad(g);
    private SilkJoystick    Wrap(SilkIJs  j) => _joysticks.TryGetValue(j, out var w) ? w : _joysticks[j]  = new SilkJoystick(j);
    private SilkInputDevice Wrap(SilkIDev d) => _others.TryGetValue(d,    out var w) ? w : _others[d]     = new SilkInputDevice(d);

    private void Purge(SilkIDev d)
    {
        if      (d is SilkIKb k) _keyboards.Remove(k);
        else if (d is SilkIMs m) _mice.Remove(m);
        else if (d is SilkIGp g) _gamepads.Remove(g);
        else if (d is SilkIJs j) _joysticks.Remove(j);
        else                     _others.Remove(d);
    }
}
