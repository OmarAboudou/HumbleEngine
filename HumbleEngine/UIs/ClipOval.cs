namespace HumbleEngine;

public partial record ClipOval : PrimitiveSingleChildWidget<Widget>
{
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

    internal override void PaintBefore(PaintCommandBuffer buffer, Position offset) =>
        buffer.Add(new PushClipOval(Rect.FromPositionAndSize(offset, Size.Value)));

    internal override void PaintAfter(PaintCommandBuffer buffer, Position offset) =>
        buffer.Add(new Pop());
}
