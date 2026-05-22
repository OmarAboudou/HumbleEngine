using SkiaSharp;

namespace HumbleEngine;

public abstract class Node
{
    private readonly List<Node> _children = new();

    public IReadOnlyList<Node> Children => _children;
    public Node? Parent { get; private set; }

    public DirtyLevel Dirty { get; private set; }
    public bool IsDirty => Dirty != DirtyLevel.None;

    public void MarkDirty(DirtyLevel level)
    {
        if (level > Dirty) Dirty = level;
    }

    public void MarkPaintDirty()  => MarkDirty(DirtyLevel.Paint);
    public void MarkLayoutDirty() => MarkDirty(DirtyLevel.Layout);
    public void MarkLogicDirty()  => MarkDirty(DirtyLevel.Logic);

    internal void ClearDirty() => Dirty = DirtyLevel.None;

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

    public virtual void Paint(SKCanvas canvas)
    {
        foreach (var child in _children)
        {
            canvas.Save();
            canvas.Translate(child.ComputedBounds.X, child.ComputedBounds.Y);
            child.Paint(canvas);
            canvas.Restore();
        }
    }

    public virtual void Dispose()
    {
        foreach (var child in _children.ToArray())
            child.Dispose();
    }

    public Rect ComputedBounds { get; protected set; }

    // Retourne la description visuelle de ce nœud pour le RenderNodeTree.
    // Les nœuds purement logiques (conteneurs sans visuel propre) retournent None.
    public virtual RenderDescription Render() => RenderDescription.None;
}
