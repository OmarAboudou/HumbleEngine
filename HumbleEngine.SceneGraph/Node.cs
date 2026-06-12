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
/// <para>
/// A node is <b>alive</b> when its ancestor chain reaches the root of a
/// <see cref="SceneTree"/>. The tree lifecycle hooks (<see cref="OnAttaching"/>,
/// <see cref="OnAttached"/>, <see cref="OnDetaching"/>, <see cref="OnDetached"/>)
/// only fire when entering or leaving a living tree — manipulating a detached
/// subtree fires nothing, and they fire on <b>every</b> entry/exit, with no hidden
/// once-only semantics.
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
    /// The living tree this node belongs to, or null when detached. Set on
    /// <b>every</b> node of the living subtree, not only its root — any node can
    /// reach its tree (e.g. <see cref="QueueDispose"/> finds the dispose queue
    /// through it). Being the root is the special case <see cref="IsTreeRoot"/>.
    /// </summary>
    public SceneTree? Tree { get; private set; }

    /// <summary>True when this node belongs to a living tree (see <see cref="Tree"/>).</summary>
    public bool IsInTree => Tree is not null;

    /// <summary>
    /// True when this node is the root of its living tree: in a tree, with no
    /// parent. Such a node is managed through <see cref="SceneTree.Root"/>, never
    /// through another node's composition.
    /// </summary>
    public bool IsTreeRoot => Tree is not null && Parent is null;

    /// <summary>
    /// Attached children, in attach order. Protected: each subclass decides whether
    /// its composition is public (containers) or private (scenes).
    /// </summary>
    protected IReadOnlyList<Node> Children => _children;

    /// <summary>Attaches a parentless node as the last child of this node.</summary>
    /// <exception cref="InvalidOperationException">The node already has a parent
    /// (use <see cref="Adopt"/> to move it), is this node itself, or is an
    /// ancestor of this node — a tree has no cycles.</exception>
    /// <exception cref="ObjectDisposedException">This node or the child is disposed.</exception>
    protected internal void Attach(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ObjectDisposedException.ThrowIf(child.IsDisposed, child);
        if (child.Parent is not null)
            throw new InvalidOperationException($"{child} already has a parent — use Adopt to move it.");
        if (child.IsTreeRoot)
            throw new InvalidOperationException($"{child} is the root of a SceneTree — manage it via SceneTree.Root.");
        EnsureNotSelfOrAncestor(child);

        _children.Add(child);
        child.Parent = this;
        child.OnParentChanged(null, this);
        if (Tree is not null)
            child.EnterTree(Tree);
    }

    /// <summary>
    /// Detaches a direct child. The child keeps living, owned from now on by
    /// whoever holds its reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is not a child of this node.</exception>
    protected internal void Detach(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child.Parent, this))
            throw new InvalidOperationException($"{child} is not a child of {this}.");

        if (child.Tree is not null)
            child.ExitTree();
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
    protected internal void Adopt(Node child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ObjectDisposedException.ThrowIf(child.IsDisposed, child);
        if (ReferenceEquals(child.Parent, this))
            return;
        if (child.IsTreeRoot)
            throw new InvalidOperationException($"{child} is the root of a SceneTree — manage it via SceneTree.Root.");
        EnsureNotSelfOrAncestor(child);

        // Tree hooks reflect facts: they fire only when tree membership actually
        // changes. Moving within the same tree (or between detached subtrees) is silent.
        var sameTree = ReferenceEquals(child.Tree, Tree);
        if (!sameTree && child.Tree is not null)
            child.ExitTree();

        var oldParent = child.Parent;
        oldParent?._children.Remove(child);
        _children.Add(child);
        child.Parent = this;
        child.OnParentChanged(oldParent, this);
        if (!sameTree && Tree is not null)
            child.EnterTree(Tree);
    }

    /// <summary>
    /// Creates a single-child slot owned by this node. Only a node can create
    /// slots for itself, so closed compositions stay closed. Expose the slot
    /// through a public property to open this part of the composition (containers);
    /// keep it private to stay closed (scenes).
    /// </summary>
    protected NodeSlot<TChild> CreateChildSlot<TChild>() where TChild : Node => new(this);

    /// <summary>
    /// Creates a typed children collection owned by this node, supporting C#
    /// collection initializers. Same closure rule as <see cref="CreateChildSlot{TChild}"/>.
    /// </summary>
    protected NodeList<TChild> CreateChildList<TChild>() where TChild : Node => new(this);

    /// <summary>
    /// Called after this node's parent changed: attached (<paramref name="oldParent"/>
    /// is null), detached (<paramref name="newParent"/> is null) or reparented.
    /// Hooks report facts only — a reparent never pretends the node left the tree.
    /// </summary>
    protected virtual void OnParentChanged(Node? oldParent, Node? newParent)
    {
    }

    /// <summary>
    /// Called when this node enters the living tree, <b>parent first</b> (top-down):
    /// the ancestors are already in the tree, the children are not notified yet.
    /// </summary>
    protected virtual void OnAttaching()
    {
    }

    /// <summary>
    /// Called when this node has entered the living tree, <b>children first</b>
    /// (bottom-up): the whole subtree below is attached and ready — safe to consume.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    /// <summary>
    /// Called when this node is about to leave the living tree, <b>parent first</b>
    /// (top-down): everything is still attached and valid — last chance to
    /// coordinate with the subtree while it lives.
    /// </summary>
    protected virtual void OnDetaching()
    {
    }

    /// <summary>
    /// Called when this node has left the living tree, <b>children first</b>
    /// (bottom-up): the subtree below has already been notified.
    /// </summary>
    protected virtual void OnDetached()
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
    /// Defers disposal to the tree's next <see cref="SceneTree.FlushDisposeQueue"/> —
    /// the safe way to destroy a node from inside hook or event code (the classic
    /// case: a handler destroying its own container). A node outside any tree is
    /// disposed immediately: there is no dispatch in flight to survive.
    /// </summary>
    public void QueueDispose()
    {
        if (IsDisposed)
            return;
        if (Tree is not null)
            Tree.EnqueueDispose(this);
        else
            Dispose();
    }

    /// <summary>
    /// Immediately disposes this node and its whole subtree, children first.
    /// Detaches from the parent beforehand. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        if (Parent is not null)
            Parent.Detach(this);
        else if (Tree is not null)
            Tree.DetachRoot();
        IsDisposed = true;
        foreach (var child in _children.ToArray())
            child.Dispose();
        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Name is null ? GetType().Name : $"{GetType().Name} '{Name}'";

    /// <summary>
    /// Recursively enters the living tree: <see cref="OnAttaching"/> fires pre-order
    /// (parent first), <see cref="OnAttached"/> post-order (children first) — a single
    /// walk yields both orders.
    /// </summary>
    internal void EnterTree(SceneTree tree)
    {
        Tree = tree;
        OnAttaching();
        foreach (var child in _children.ToArray())
            child.EnterTree(tree);
        OnAttached();
    }

    /// <summary>
    /// Recursively leaves the living tree: <see cref="OnDetaching"/> fires pre-order
    /// (parent first, everything still valid), <see cref="OnDetached"/> post-order
    /// (children first); membership is cleared last.
    /// </summary>
    internal void ExitTree()
    {
        OnDetaching();
        foreach (var child in _children.ToArray())
            child.ExitTree();
        OnDetached();
        Tree = null;
    }

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