using HumbleEngine;

namespace HumbleEngine.Silk;

public class SilkApplication : Application
{
    protected override PlatformWindow CreatePlatformWindow(ApplicationConfig config) =>
        new SilkWindow(config.Width, config.Height);
}
