namespace HumbleEngine;

public partial record ConstrainedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<BoxConstraints> AdditionalConstraints { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        BoxConstraints childConstraints = constraints.Enforce(AdditionalConstraints.Value);

        if (Child.Value is PrimitiveWidget child)
        {
            child.Layout(childConstraints);
            Size.Value = child.Size.Value;
        }
        else
        {
            Size.Value = new Size(childConstraints.MinWidth, childConstraints.MinHeight);
        }
    }
}
