using HumbleEngine;
using SilkMon = Silk.NET.Windowing.IMonitor;

namespace HumbleEngine.Silk;

public class SilkMonitor : IMonitor
{
    private readonly SilkMon _monitor;

    public SilkMonitor(SilkMon monitor, bool isPrimary)
    {
        _monitor   = monitor;
        IsPrimary  = isPrimary;
    }

    public int          Index       => _monitor.Index;
    public string       Name        => _monitor.Name;
    public bool         IsPrimary   { get; }
    public Vector2<int> Position    => new(_monitor.Bounds.Origin.X, _monitor.Bounds.Origin.Y);
    public Vector2<int> Size        => new(_monitor.Bounds.Size.X,   _monitor.Bounds.Size.Y);
    public int          RefreshRate => _monitor.VideoMode.RefreshRate ?? 60;
}
