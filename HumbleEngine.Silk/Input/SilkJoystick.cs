using HumbleEngine;
using SilkJs = Silk.NET.Input.IJoystick;

namespace HumbleEngine.Silk;

public class SilkJoystick : IJoystick
{
    private readonly SilkJs _joystick;

    public SilkJoystick(SilkJs joystick) => _joystick = joystick;

    public string Name        => _joystick.Name;
    public int    Index       => _joystick.Index;
    public bool   IsConnected => _joystick.IsConnected;
}
