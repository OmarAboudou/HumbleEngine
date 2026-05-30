namespace HumbleEngine;

public partial record Padding : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<EdgeInsets> Insets { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        EdgeInsets insets = Insets.Value;
        float horizontal  = insets.Horizontal;
        float vertical    = insets.Vertical;

        if (Child.Value is PrimitiveWidget child)
        {
            child.Layout(constraints.Deflate(horizontal, vertical));
            Size.Value = new Size(
                child.Size.Value.Width  + horizontal,
                child.Size.Value.Height + vertical
            );
        }
        else
        {
            Size.Value = constraints.Constrain(new Size(horizontal, vertical));
        }
    }
}
