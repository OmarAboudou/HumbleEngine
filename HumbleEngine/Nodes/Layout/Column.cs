namespace HumbleEngine;

public class Column : Node
{
    public ReactiveProperty<float> Spacing = new(0f);

    public override void Init()
    {
        base.Init();
        Spacing.Connect(_ => MarkLayoutDirty());
    }

    protected override RenderDescription RenderContent()
    {
        VLayout layout = [..Children.Select(c => c.Render())];
        return layout.Spacing(Spacing.Value);
    }
}
