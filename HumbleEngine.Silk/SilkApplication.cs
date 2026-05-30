using HumbleEngine;

namespace HumbleEngine.Silk;

public class SilkApplication : Application
{
    protected override PlatformWindow CreatePlatformWindow()
    {
        return new SilkWindow();
    }
}