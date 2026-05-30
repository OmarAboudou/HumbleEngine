namespace HumbleEngine;

public partial record Align : PrimitiveSingleChildWidget<Widget>
{
    // default(Alignment) = (0, 0) = Alignment.Center
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<Alignment> Alignment { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> WidthFactor { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> HeightFactor { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        if (Child.Value is PrimitiveWidget child)
        {
            child.Layout(constraints.Loosen());

            Size childSize = child.Size.Value;

            float myWidth  = ResolveAxis(constraints.MaxWidth,  childSize.Width,  WidthFactor.Value);
            float myHeight = ResolveAxis(constraints.MaxHeight, childSize.Height, HeightFactor.Value);

            myWidth  = constraints.WidthConstraints.Constrain(myWidth);
            myHeight = constraints.HeightConstraints.Constrain(myHeight);

            Alignment align = Alignment.Value;
            child.LocalPosition.Value = new Position(
                (align.X + 1f) / 2f * (myWidth  - childSize.Width),
                (align.Y + 1f) / 2f * (myHeight - childSize.Height)
            );

            Size.Value = new Size(myWidth, myHeight);
        }
        else
        {
            float myWidth  = float.IsPositiveInfinity(constraints.MaxWidth)  ? 0f : constraints.MaxWidth;
            float myHeight = float.IsPositiveInfinity(constraints.MaxHeight) ? 0f : constraints.MaxHeight;
            Size.Value = constraints.Constrain(new Size(myWidth, myHeight));
        }
    }

    // Axe borné sans factor → prend le max disponible.
    // Axe non borné sans factor → s'adapte à l'enfant.
    // Avec factor → child * factor (quelle que soit la contrainte).
    private static float ResolveAxis(float maxConstraint, float childSize, float? factor) =>
        factor is { } f ? childSize * f
            : float.IsPositiveInfinity(maxConstraint) ? childSize
            : maxConstraint;
}
