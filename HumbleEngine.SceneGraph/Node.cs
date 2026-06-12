namespace HumbleEngine;

/// <summary>
/// Base element of the scene tree: a parent, an ordered list of children, and a
/// lifecycle. The base node carries no transform — spatial models belong to
/// specialized node types.
/// <para>
/// Composition is <b>closed by default</b>: children are managed through protected
/// members, and each subclass decides whether to expose them publicly (containers
/// do, scenes never do). Encapsulation is ultimately enforced by reference privacy —
/// code that holds no reference to a node cannot reach it.
/// </para>
/// <para>
/// Ownership follows the tree: disposing a node disposes its whole subtree,
/// children first. A detached node is owned by whoever holds its reference.
/// </para>
/// </summary>
public abstract class Node : IDisposable
{
    private readonly List<Node> _children = [];

    /// <summary>
    /// Optional label for debugging and tooling — never an identifier: dependencies
    /// are injected, never looked up by name or path. Not unique among siblings.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Node this node is attached to, or null when detached. Structural information
    /// only — behavior must not depend on the parent's concrete type: legitimate
    /// needs flow through framework protocols, dependencies are injected.
    /// </summary>
    public Node? Parent { get; private set; }

    /// <summary>True once <see cref="Dispose"/> has run. A disposed node cannot be attached again.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Attached children, in attach order. Protected: each subclass decides whether
    /// its composition is public (containers) or private (scenes).
    /// </summary>
    protected IReadOnlyList<Node> Children => _children;

    /// <summary>Attaches a parentless node as the last child of this node.</summary>
    /// <exception cref="InvalidOperationException">The node already has a parent
    /// (use <see cref="Reparent"/> to move it), is this node itself, or is an
    /// ancestor of this node — a tree has no cycles.</exception>
    /// <exception cref="ObjectDisposedException">This node or the child is disposed.</exception>
    protected void Attach(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ObjectDisposedException.ThrowIf(child.IsDisposed, child);
        if (child.Parent is not null)
            throw new InvalidOperationException($"{child} already has a parent — use Reparent to move it.");
        EnsureNotSelfOrAncestor(child);

        _children.Add(child);
        child.Parent = this;
        child.OnParentChanged(null, this);
    }

    /// <summary>
    /// Detaches a direct child. The child keeps living, owned from now on by
    /// whoever holds its reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is not a child of this node.</exception>
    protected void Detach(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child.Parent, this))
            throw new InvalidOperationException($"{child} is not a child of {this}.");

        _children.Remove(child);
        child.Parent = null;
        child.OnParentChanged(this, null);
    }

    /// <summary>
    /// Moves a node from wherever it sits to be the last child of this node, as a
    /// single operation: fires <see cref="OnParentChanged"/> exactly once and does
    /// not replay the attach/detach lifecycle — moving is not leaving, state is
    /// preserved. Accepts a parentless node; no-op when already a child of this node.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is this node itself or
    /// an ancestor of this node — a tree has no cycles.</exception>
    /// <exception cref="ObjectDisposedException">This node or the child is disposed.</exception>
    protected void Reparent(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ObjectDisposedException.ThrowIf(child.IsDisposed, child);
        if (ReferenceEquals(child.Parent, this))
            return;
        EnsureNotSelfOrAncestor(child);

        var oldParent = child.Parent;
        oldParent?._children.Remove(child);
        _children.Add(child);
        child.Parent = this;
        child.OnParentChanged(oldParent, this);
    }

    /// <summary>
    /// Called after this node's parent changed: attached (<paramref name="oldParent"/>
    /// is null), detached (<paramref name="newParent"/> is null) or reparented.
    /// Hooks report facts only — a reparent never pretends the node left the tree.
    /// </summary>
    protected virtual void OnParentChanged(Node? oldParent, Node? newParent)
    {
    }

    /// <summary>
    /// Called once while disposing, after all children are disposed (bottom-up) —
    /// release non-tree resources here.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Immediately disposes this node and its whole subtree, children first.
    /// Detaches from the parent beforehand. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        Parent?.Detach(this);
        IsDisposed = true;
        foreach (var child in _children.ToArray())
            child.Dispose();
        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Name is null ? GetType().Name : $"{GetType().Name} '{Name}'";

    /// <summary>Rejects a child that is this node itself or one of its ancestors.</summary>
    private void EnsureNotSelfOrAncestor(Node child)
    {
        for (var ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, child))
                throw new InvalidOperationException($"Attaching {child} under {this} would create a cycle.");
        }
    }
}