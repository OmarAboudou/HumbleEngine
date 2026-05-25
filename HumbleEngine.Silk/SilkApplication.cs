using HumbleEngine.Core;

namespace HumbleEngine.Silk;

public class SilkApplication : Application
{
    protected override Window CreateWindow() 
        => new SilkWindow();
}