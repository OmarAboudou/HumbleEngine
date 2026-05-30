namespace HumbleEngine;

public partial record ConstrainedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<BoxConstraints> AdditionalConstraints { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        BoxConstraints childConstraints = constraints.Enforce(AdditionalConstraints.Value);

        if (MountedChildren.Count > 0)
        {
            Widget child = MountedChildren[0];
            child.Layout(childConstraints);
            Size.Value = child.GetSize();
        }
        else
        {
            Size.Value = new Size(childConstraints.MinWidth, childConstraints.MinHeight);
        }
    }
}
