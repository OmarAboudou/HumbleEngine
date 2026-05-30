namespace HumbleEngine;

public partial record DecoratedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<BoxDecoration> Decoration { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.PAINT)]
    public partial Property<DecorationPosition> Position { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        if (MountedChildren.Count > 0)
        {
            Widget child = MountedChildren[0];
            child.Layout(constraints);
            Size.Value = child.GetSize();
        }
        else
        {
            Size.Value = constraints.Constrain(new Size(0f, 0f));
        }
    }

    internal override void PaintBefore(PaintCommandBuffer buffer, Position offset)
    {
        if (Position.Value == DecorationPosition.Background)
            PaintDecoration(buffer, Rect.FromPositionAndSize(offset, Size.Value), Decoration.Value);
    }

    internal override void PaintAfter(PaintCommandBuffer buffer, Position offset)
    {
        if (Position.Value == DecorationPosition.Foreground)
            PaintDecoration(buffer, Rect.FromPositionAndSize(offset, Size.Value), Decoration.Value);
    }

    private static void PaintDecoration(PaintCommandBuffer buffer, Rect bounds, BoxDecoration deco)
    {
        foreach (BoxShadow shadow in deco.Shadows)
            buffer.Add(new DrawShadow(bounds, deco.BorderRadius, shadow.Color,
                                      shadow.BlurRadius, shadow.SpreadRadius,
                                      shadow.OffsetX, shadow.OffsetY));

        if (deco.Color is { } color)
            buffer.Add(new FillRRect(bounds, deco.BorderRadius, color));

        if (deco.Border is { } border && border.IsUniform && border.Top.Width > 0f)
            buffer.Add(new StrokeRRect(bounds, deco.BorderRadius, border.Top.Color, border.Top.Width));
    }
}
