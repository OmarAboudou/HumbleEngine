using HumbleEngine;
using HumbleEngine.Silk;
using HumbleEngine.Skia;

namespace HumbleEngine.Demo;

public class DemoApp : SilkApplication
{
    protected override IRenderer? CreateRenderer(GraphicsAPI api) => new SkiaRenderer();
}
