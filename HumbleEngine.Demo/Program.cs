using HumbleEngine;
using HumbleEngine.Demo;

var scene  = new DemoScene();
var config = ApplicationConfig.Default(scene) with
{
    WindowOptions = new WindowOptions("HumbleEngine Demo", new Vector2<int>(1280, 720)),
    Passes        = [new FixedUpdatePass(), new AnimationPass(), new UIRenderPass(), new InputPass()],
};

new DemoApp().Run(config);
