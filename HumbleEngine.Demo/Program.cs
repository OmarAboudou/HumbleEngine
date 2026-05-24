using HumbleEngine;
using HumbleEngine.Demo;

var scene  = new DemoScene();
var config = ApplicationConfig.Default(scene) with
{
    WindowOptions = new WindowOptions("HumbleEngine Demo", new Vector2<int>(1280, 720)),
    Passes        = [new UpdatePass(), new UIRenderPass()],
};

new DemoApp().Run(config);
