using HumbleEngine;
using SilkDev = Silk.NET.Input.IInputDevice;

namespace HumbleEngine.Silk;

public class SilkInputDevice : IInputDevice
{
    private readonly SilkDev _device;

    public SilkInputDevice(SilkDev device) => _device = device;

    public string Name        => _device.Name;
    public int    Index       => _device.Index;
    public bool   IsConnected => _device.IsConnected;
}
