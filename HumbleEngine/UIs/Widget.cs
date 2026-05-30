namespace HumbleEngine;

public abstract partial record Widget : HumbleRecord
{
    public object? Key { get; init; }
    public object? GlobalKey { get; init; }

    internal Widget? Parent { get; set; }
    internal List<Widget> MountedChildren { get; } = [];
    internal virtual IReadOnlyList<Widget> GetChildren() => [];

    public abstract void Layout(BoxConstraints constraints);
    internal abstract Size GetSize();

    internal virtual void SetLocalPosition(Position position)
    {
        if (MountedChildren.Count > 0)
            MountedChildren[0].SetLocalPosition(position);
    }

    protected virtual void OnMount() { }
    protected virtual void OnUnmount() { }
}
