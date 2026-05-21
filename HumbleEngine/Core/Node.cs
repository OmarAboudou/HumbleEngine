namespace HumbleEngine;

public abstract class Node
{
    private readonly List<Node> _children = new();

    public IReadOnlyList<Node> Children => _children;
    public Node? Parent { get; private set; }

    public bool IsDirty { get; private set; }

    public void MarkDirty() => IsDirty = true;

    internal void ClearDirty() => IsDirty = false;

    public void AddChild(Node child)
    {
        if (child.Parent is not null)
            throw new InvalidOperationException("Node already has a parent.");

        _children.Add(child);
        child.Parent = this;
        child.Init();
    }

    public void RemoveChild(Node child)
    {
        if (!_children.Remove(child)) return;
        child.Parent = null;
        child.Dispose();
    }

    public virtual void Init() { }

    public virtual void Update(float delta)
    {
        foreach (var child in _children.ToArray())
            child.Update(delta);
    }

    public virtual void Layout(Size available)
    {
        foreach (var child in _children)
            child.Layout(available);
    }

    public virtual void Dispose()
    {
        foreach (var child in _children.ToArray())
            child.Dispose();
    }

    public Rect ComputedBounds { get; protected set; }
}
