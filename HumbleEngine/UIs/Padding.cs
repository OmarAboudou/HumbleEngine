namespace HumbleEngine;

public partial record Padding : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<EdgeInsets> Insets { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        EdgeInsets insets     = Insets.Value;
        float      horizontal = insets.Horizontal;
        float      vertical   = insets.Vertical;

        if (MountedChildren.Count > 0)
        {
            Widget child = MountedChildren[0];
            child.Layout(constraints.Deflate(horizontal, vertical));
            child.SetLocalPosition(new Position(insets.Left, insets.Top));
            Size childSize = child.GetSize();
            Size.Value = new Size(childSize.Width + horizontal, childSize.Height + vertical);
        }
        else
        {
            Size.Value = constraints.Constrain(new Size(horizontal, vertical));
        }
    }
}
