using HumbleEngine;
using HumbleEngine.Silk;
using HumbleEngine.Skia;

new TestApplication().Run(new ApplicationConfig
{
    Title            = "HumbleEngine — Layout Test",
    Width            = 800,
    Height           = 600,
    PreferredBackend = GPUBackend.OpenGL,
});

class TestApplication : SilkApplication
{
    protected override IRenderer? CreateRenderer() => new SkiaRenderer();

    protected override Widget? BuildRootWidget()
    {
        PaintPass.DebugFill = true;

        // Arbre de test :
        //  SizedBox(500×350)
        //    Padding(30px)
        //      Align(BottomRight)
        //        SizedBox(150×80)

        var inner = new SizedBox();
        inner.Width.Value  = 150f;
        inner.Height.Value = 80f;

        var align = new Align();
        align.Alignment.Value = Alignment.BottomRight;
        align.Child.Value     = inner;

        var padding = new Padding();
        padding.Insets.Value = EdgeInsets.All(30f);
        padding.Child.Value  = align;

        var outer = new SizedBox();
        outer.Width.Value  = 500f;
        outer.Height.Value = 350f;
        outer.Child.Value  = padding;

        return outer;
    }
}
