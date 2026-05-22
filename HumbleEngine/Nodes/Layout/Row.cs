namespace HumbleEngine;

public class Row : Node
{
    public ReactiveProperty<float> Spacing = new(0f);

    public override void Init()
    {
        base.Init();
        Spacing.Connect(_ => MarkLayoutDirty());
    }

    protected override RenderDescription RenderContent()
    {
        var layout = new HLayout().Spacing(Spacing.Value);
        foreach (var child in Children)
            layout.Add(child.Render());
        return layout;
    }
}
