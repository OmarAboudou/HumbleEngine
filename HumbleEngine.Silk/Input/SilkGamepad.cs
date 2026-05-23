using HumbleEngine;
using SilkGp = Silk.NET.Input.IGamepad;

namespace HumbleEngine.Silk;

public class SilkGamepad : IGamepad
{
    private readonly SilkGp _gamepad;

    public SilkGamepad(SilkGp gamepad) => _gamepad = gamepad;

    public string Name        => _gamepad.Name;
    public int    Index       => _gamepad.Index;
    public bool   IsConnected => _gamepad.IsConnected;
}
