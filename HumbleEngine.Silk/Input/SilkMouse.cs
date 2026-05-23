using System.Collections.Generic;
using System.Linq;
using HumbleEngine;
using SilkMse = Silk.NET.Input.IMouse;
using SilkBtn = Silk.NET.Input.MouseButton;

namespace HumbleEngine.Silk;

public class SilkMouse : IMouse
{
    private readonly SilkMse _mouse;

    private readonly Signal<MouseButton>                 _onButtonDown  = new();
    private readonly Signal<MouseButton>                 _onButtonUp    = new();
    private readonly Signal<MouseButton, Vector2<float>> _onClick       = new();
    private readonly Signal<MouseButton, Vector2<float>> _onDoubleClick = new();
    private readonly Signal<Vector2<float>>              _onMove        = new();
    private readonly Signal<ScrollWheel>                 _onScroll      = new();

    public SilkMouse(SilkMse mouse)
    {
        _mouse = mouse;
        _mouse.MouseDown   += (_, b)    => _onButtonDown.Emit(FromSilk(b));
        _mouse.MouseUp     += (_, b)    => _onButtonUp.Emit(FromSilk(b));
        _mouse.Click       += (_, b, p) => _onClick.Emit(FromSilk(b), new Vector2<float>(p.X, p.Y));
        _mouse.DoubleClick += (_, b, p) => _onDoubleClick.Emit(FromSilk(b), new Vector2<float>(p.X, p.Y));
        _mouse.MouseMove   += (_, p)    => _onMove.Emit(new Vector2<float>(p.X, p.Y));
        _mouse.Scroll      += (_, w)    => _onScroll.Emit(new ScrollWheel(w.X, w.Y));
    }

    public IReadOnlyList<MouseButton> SupportedButtons => _mouse.SupportedButtons.Select(FromSilk).ToList();
    public IReadOnlyList<ScrollWheel> ScrollWheels     => _mouse.ScrollWheels.Select(w => new ScrollWheel(w.X, w.Y)).ToList();

    public Vector2<float> Position
    {
        get => new(_mouse.Position.X, _mouse.Position.Y);
        set => _mouse.Position = new System.Numerics.Vector2(value.X, value.Y);
    }

    public ICursor Cursor           => new SilkCursor(_mouse.Cursor);
    public int     DoubleClickTime  { get => _mouse.DoubleClickTime;  set => _mouse.DoubleClickTime  = value; }
    public int     DoubleClickRange { get => _mouse.DoubleClickRange; set => _mouse.DoubleClickRange = value; }

    public bool IsButtonPressed(MouseButton button) => _mouse.IsButtonPressed(ToSilk(button));

    public IReadOnlySignal<MouseButton>                 OnButtonDown  => _onButtonDown.AsReadOnly();
    public IReadOnlySignal<MouseButton>                 OnButtonUp    => _onButtonUp.AsReadOnly();
    public IReadOnlySignal<MouseButton, Vector2<float>> OnClick       => _onClick.AsReadOnly();
    public IReadOnlySignal<MouseButton, Vector2<float>> OnDoubleClick => _onDoubleClick.AsReadOnly();
    public IReadOnlySignal<Vector2<float>>              OnMove        => _onMove.AsReadOnly();
    public IReadOnlySignal<ScrollWheel>                 OnScroll      => _onScroll.AsReadOnly();

    private static MouseButton FromSilk(SilkBtn b) => b switch
    {
        SilkBtn.Left     => MouseButton.Left,
        SilkBtn.Right    => MouseButton.Right,
        SilkBtn.Middle   => MouseButton.Middle,
        SilkBtn.Button4  => MouseButton.Button4,
        SilkBtn.Button5  => MouseButton.Button5,
        SilkBtn.Button6  => MouseButton.Button6,
        SilkBtn.Button7  => MouseButton.Button7,
        SilkBtn.Button8  => MouseButton.Button8,
        SilkBtn.Button9  => MouseButton.Button9,
        SilkBtn.Button10 => MouseButton.Button10,
        SilkBtn.Button11 => MouseButton.Button11,
        SilkBtn.Button12 => MouseButton.Button12,
        _                => MouseButton.Unknown,
    };

    private static SilkBtn ToSilk(MouseButton b) => b switch
    {
        MouseButton.Left     => SilkBtn.Left,
        MouseButton.Right    => SilkBtn.Right,
        MouseButton.Middle   => SilkBtn.Middle,
        MouseButton.Button4  => SilkBtn.Button4,
        MouseButton.Button5  => SilkBtn.Button5,
        MouseButton.Button6  => SilkBtn.Button6,
        MouseButton.Button7  => SilkBtn.Button7,
        MouseButton.Button8  => SilkBtn.Button8,
        MouseButton.Button9  => SilkBtn.Button9,
        MouseButton.Button10 => SilkBtn.Button10,
        MouseButton.Button11 => SilkBtn.Button11,
        MouseButton.Button12 => SilkBtn.Button12,
        _                    => SilkBtn.Unknown,
    };
}
