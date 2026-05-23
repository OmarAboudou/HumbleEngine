using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkJs     = Silk.NET.Input.IJoystick;
using SilkBtn    = Silk.NET.Input.Button;
using SilkAxis   = Silk.NET.Input.Axis;
using SilkHat    = Silk.NET.Input.Hat;
using SilkPos2D  = Silk.NET.Input.Position2D;
using SilkDz     = Silk.NET.Input.Deadzone;
using SilkDzMeth = Silk.NET.Input.DeadzoneMethod;
using SilkBtnName = Silk.NET.Input.ButtonName;

namespace HumbleEngine.Silk;

public class SilkJoystick : IJoystick
{
    private readonly SilkJs _joystick;

    private readonly Signal<GamepadButton> _onButtonDown = new();
    private readonly Signal<GamepadButton> _onButtonUp   = new();
    private readonly Signal<Axis>          _onAxisMoved  = new();
    private readonly Signal<Hat>           _onHatMoved   = new();

    public SilkJoystick(SilkJs joystick)
    {
        _joystick = joystick;
        _joystick.ButtonDown += (_, b) => _onButtonDown.Emit(FromSilk(b));
        _joystick.ButtonUp   += (_, b) => _onButtonUp.Emit(FromSilk(b));
        _joystick.AxisMoved  += (_, a) => _onAxisMoved.Emit(FromSilk(a));
        _joystick.HatMoved   += (_, h) => _onHatMoved.Emit(FromSilk(h));
    }

    public string Name        => _joystick.Name;
    public int    Index       => _joystick.Index;
    public bool   IsConnected => _joystick.IsConnected;

    public IReadOnlyList<Axis>          Axes    => _joystick.Axes.Select(FromSilk).ToList();
    public IReadOnlyList<GamepadButton> Buttons => _joystick.Buttons.Select(FromSilk).ToList();
    public IReadOnlyList<Hat>           Hats    => _joystick.Hats.Select(FromSilk).ToList();

    public Deadzone Deadzone
    {
        get => FromSilk(_joystick.Deadzone);
        set => _joystick.Deadzone = ToSilk(value);
    }

    public IReadOnlySignal<GamepadButton> OnButtonDown => _onButtonDown.AsReadOnly();
    public IReadOnlySignal<GamepadButton> OnButtonUp   => _onButtonUp.AsReadOnly();
    public IReadOnlySignal<Axis>          OnAxisMoved  => _onAxisMoved.AsReadOnly();
    public IReadOnlySignal<Hat>           OnHatMoved   => _onHatMoved.AsReadOnly();

    private static GamepadButton FromSilk(SilkBtn b)  => new(FromSilk(b.Name), b.Index, b.Pressed);
    private static Axis          FromSilk(SilkAxis a) => new(a.Index, a.Position);
    private static Hat           FromSilk(SilkHat h)  => new(h.Index, FromSilk(h.Position));
    private static Deadzone      FromSilk(SilkDz d)   => new(d.Value, FromSilk(d.Method));
    private static SilkDz        ToSilk(Deadzone d)   => new(d.Value, ToSilk(d.Method));

    private static ButtonName FromSilk(SilkBtnName n) => n switch
    {
        SilkBtnName.A           => ButtonName.A,
        SilkBtnName.B           => ButtonName.B,
        SilkBtnName.X           => ButtonName.X,
        SilkBtnName.Y           => ButtonName.Y,
        SilkBtnName.LeftBumper  => ButtonName.LeftBumper,
        SilkBtnName.RightBumper => ButtonName.RightBumper,
        SilkBtnName.Back        => ButtonName.Back,
        SilkBtnName.Start       => ButtonName.Start,
        SilkBtnName.Home        => ButtonName.Home,
        SilkBtnName.LeftStick   => ButtonName.LeftStick,
        SilkBtnName.RightStick  => ButtonName.RightStick,
        SilkBtnName.DPadUp      => ButtonName.DPadUp,
        SilkBtnName.DPadRight   => ButtonName.DPadRight,
        SilkBtnName.DPadDown    => ButtonName.DPadDown,
        SilkBtnName.DPadLeft    => ButtonName.DPadLeft,
        _                       => ButtonName.Unknown,
    };

    private static HatPosition FromSilk(SilkPos2D p) => p switch
    {
        SilkPos2D.Up        => HatPosition.Up,
        SilkPos2D.Down      => HatPosition.Down,
        SilkPos2D.Left      => HatPosition.Left,
        SilkPos2D.Right     => HatPosition.Right,
        SilkPos2D.UpLeft    => HatPosition.UpLeft,
        SilkPos2D.UpRight   => HatPosition.UpRight,
        SilkPos2D.DownLeft  => HatPosition.DownLeft,
        SilkPos2D.DownRight => HatPosition.DownRight,
        _                   => HatPosition.Centered,
    };

    private static DeadzoneMethod FromSilk(SilkDzMeth m) => m switch
    {
        SilkDzMeth.AdaptiveGradient => DeadzoneMethod.AdaptiveGradient,
        _                           => DeadzoneMethod.Traditional,
    };

    private static SilkDzMeth ToSilk(DeadzoneMethod m) => m switch
    {
        DeadzoneMethod.AdaptiveGradient => SilkDzMeth.AdaptiveGradient,
        _                               => SilkDzMeth.Traditional,
    };
}
