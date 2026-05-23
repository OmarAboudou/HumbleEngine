using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkGp      = Silk.NET.Input.IGamepad;
using SilkBtn     = Silk.NET.Input.Button;
using SilkStick   = Silk.NET.Input.Thumbstick;
using SilkTrig    = Silk.NET.Input.Trigger;
using SilkDz      = Silk.NET.Input.Deadzone;
using SilkDzMeth  = Silk.NET.Input.DeadzoneMethod;
using SilkBtnName = Silk.NET.Input.ButtonName;

namespace HumbleEngine.Silk;

public class SilkGamepad : IGamepad
{
    private readonly SilkGp            _gamepad;
    private readonly IReadOnlyList<IMotor> _motors;

    private readonly Signal<GamepadButton> _onButtonDown      = new();
    private readonly Signal<GamepadButton> _onButtonUp        = new();
    private readonly Signal<Thumbstick>    _onThumbstickMoved = new();
    private readonly Signal<Trigger>       _onTriggerMoved    = new();

    public SilkGamepad(SilkGp gamepad)
    {
        _gamepad = gamepad;
        _motors  = gamepad.VibrationMotors.Select(m => (IMotor)new SilkMotor(m)).ToList();

        _gamepad.ButtonDown      += (_, b) => _onButtonDown.Emit(FromSilk(b));
        _gamepad.ButtonUp        += (_, b) => _onButtonUp.Emit(FromSilk(b));
        _gamepad.ThumbstickMoved += (_, s) => _onThumbstickMoved.Emit(FromSilk(s));
        _gamepad.TriggerMoved    += (_, t) => _onTriggerMoved.Emit(FromSilk(t));
    }

    public string Name        => _gamepad.Name;
    public int    Index       => _gamepad.Index;
    public bool   IsConnected => _gamepad.IsConnected;

    public IReadOnlyList<GamepadButton> Buttons        => _gamepad.Buttons.Select(FromSilk).ToList();
    public IReadOnlyList<Thumbstick>    Thumbsticks    => _gamepad.Thumbsticks.Select(FromSilk).ToList();
    public IReadOnlyList<Trigger>       Triggers       => _gamepad.Triggers.Select(FromSilk).ToList();
    public IReadOnlyList<IMotor>        VibrationMotors => _motors;

    public Deadzone Deadzone
    {
        get => FromSilk(_gamepad.Deadzone);
        set => _gamepad.Deadzone = ToSilk(value);
    }

    public IReadOnlySignal<GamepadButton> OnButtonDown      => _onButtonDown.AsReadOnly();
    public IReadOnlySignal<GamepadButton> OnButtonUp        => _onButtonUp.AsReadOnly();
    public IReadOnlySignal<Thumbstick>    OnThumbstickMoved => _onThumbstickMoved.AsReadOnly();
    public IReadOnlySignal<Trigger>       OnTriggerMoved    => _onTriggerMoved.AsReadOnly();

    private static GamepadButton FromSilk(SilkBtn b)   => new(FromSilk(b.Name), b.Index, b.Pressed);
    private static Thumbstick    FromSilk(SilkStick s)  => new(s.Index, s.X, s.Y);
    private static Trigger       FromSilk(SilkTrig t)   => new(t.Index, t.Position);
    private static Deadzone      FromSilk(SilkDz d)     => new(d.Value, FromSilk(d.Method));
    private static SilkDz        ToSilk(Deadzone d)     => new(d.Value, ToSilk(d.Method));

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
