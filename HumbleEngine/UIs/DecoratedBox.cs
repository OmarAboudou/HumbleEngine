namespace HumbleEngine;

public partial record DecoratedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<BoxDecoration> Decoration { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<DecorationPosition> Position { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        if (Child.Value is PrimitiveWidget child)
        {
            child.Layout(constraints);
            Size.Value = child.Size.Value;
        }
        else
        {
            Size.Value = constraints.Constrain(new Size(0f, 0f));
        }
    }

    // Override Paint directement pour contrôler l'ordre décoration / enfants.
    internal override void Paint(PaintCommandBuffer buffer, Position offset)
    {
        Rect          bounds = Rect.FromPositionAndSize(offset, Size.Value);
        BoxDecoration deco   = Decoration.Value;

        if (Position.Value == DecorationPosition.Background)
            PaintDecoration(buffer, bounds, deco);

        base.Paint(buffer, offset);  // Widget.Paint() → peint MountedChildren

        if (Position.Value == DecorationPosition.Foreground)
            PaintDecoration(buffer, bounds, deco);
    }

    private static void PaintDecoration(PaintCommandBuffer buffer, Rect bounds, BoxDecoration deco)
    {
        foreach (BoxShadow shadow in deco.Shadows)
            buffer.Add(new DrawShadow(bounds, deco.BorderRadius, shadow.Color,
                                      shadow.BlurRadius, shadow.SpreadRadius,
                                      shadow.OffsetX, shadow.OffsetY));

        if (deco.Color is { } color)
            buffer.Add(new FillRRect(bounds, deco.BorderRadius, color));

        if (deco.Border is { } border)
        {
            if (border.IsUniform && border.Top.Width > 0f)
                buffer.Add(new StrokeRRect(bounds, deco.BorderRadius, border.Top.Color, border.Top.Width));
        }
    }
}
