using HumbleEngine.Core;

namespace HumbleEngine.Silk;

public class SilkApplication : Application
{
    protected override PlatformWindow CreatePlatformWindow() 
        => new SilkWindow();
}