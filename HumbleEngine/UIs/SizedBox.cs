namespace HumbleEngine;

public partial record SizedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Width { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Height { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        float targetWidth  = Width.Value  is { } w ? constraints.WidthConstraints.Constrain(w)  : constraints.MinWidth;
        float targetHeight = Height.Value is { } h ? constraints.HeightConstraints.Constrain(h) : constraints.MinHeight;

        if (MountedChildren.Count > 0)
        {
            Widget child = MountedChildren[0];
            BoxConstraints childConstraints = new(
                Width.Value  is not null ? LengthConstraints.Tight(targetWidth)  : constraints.WidthConstraints,
                Height.Value is not null ? LengthConstraints.Tight(targetHeight) : constraints.HeightConstraints
            );
            child.Layout(childConstraints);

            if (Width.Value  is null) targetWidth  = child.GetSize().Width;
            if (Height.Value is null) targetHeight = child.GetSize().Height;
        }

        Size.Value = new Size(targetWidth, targetHeight);
    }
}
