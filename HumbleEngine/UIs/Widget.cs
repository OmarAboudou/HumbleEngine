namespace HumbleEngine;

public abstract partial record Widget : HumbleRecord
{
    public object? Key { get; init; }
    public object? GlobalKey { get; init; }

    internal Widget? Parent { get; set; }
    internal List<Widget> MountedChildren { get; } = [];
    internal virtual IReadOnlyList<Widget> GetChildren() => [];

    protected virtual void OnMount() { }
    protected virtual void OnUnmount() { }

    internal virtual void Paint(PaintCommandBuffer buffer, Position offset)
    {
        foreach (Widget child in MountedChildren)
            if (child is PrimitiveWidget primitive)
                primitive.Paint(buffer, offset + primitive.LocalPosition.Value);
            else
                child.Paint(buffer, offset);
    }
}