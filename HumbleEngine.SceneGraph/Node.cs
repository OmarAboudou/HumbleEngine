namespace HumbleEngine;

/// <summary>
/// Base element of the scene tree: a parent, an ordered list of children, and a
/// lifecycle. The base node carries no transform — spatial models belong to
/// specialized node types.
/// <para>
/// Composition is <b>closed to writing, open to reading</b>: the children
/// (<see cref="Children"/>) are a public read-only, observable view of the real
/// subtree — any node can be introspected, which is what the editor's hierarchy
/// reads — but <i>mutating</i> a composition goes only through the owner's own
/// slots/lists, which each subclass decides to expose (containers do, scenes keep
/// them private). Integrity is enforced by the type: the view has no write path, so
/// the interior is readable, never writable, from the outside.
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
    private readonly SourceObservers _childrenStructure = new();
    private ReadOnlyNodeList? _childrenView;
    private Dictionary<Type, object>? _provided;

    /// <summary>
    /// Internal broadcast of a child's arrival at the given index — the symmetric
    /// counterpart of <see cref="ChildDeparted"/>. Raised once the child is fully
    /// attached (parent set, tree entered), so observers see a coherent tree. The
    /// public <see cref="Children"/> view relays it as an insertion.
    /// </summary>
    internal event Action<int, Node>? ChildInserted;

    /// <summary>
    /// Internal broadcast of a child's departure from the given index — explicit
    /// detach, adoption elsewhere and disposal all funnel through the owner's
    /// machinery, which raises this. Two audiences read it: the slots and lists this
    /// node created (they ignore the index and locate the departing child in their
    /// own partial view), and the exhaustive <see cref="Children"/> view (which uses
    /// the index to narrate a removal). The multicast delegate is the registry.
    /// </summary>
    internal event Action<int, Node>? ChildDeparted;

    /// <summary>
    /// Internal broadcast of this node's death: cells and lists created by this
    /// node subscribe their binding release here, so bindings live exactly as
    /// long as their owner. Subscriber and owner share one lifetime — this wiring
    /// can never leak.
    /// </summary>
    internal event Action? Disposing;

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
    public Node? Parent
    {
        get => _parent;
        private set => _parent = value;
    }

    private Node? _parent;

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
    /// Attached children, in attach order — a <b>read-only, observable</b> view of
    /// the real subtree. Public and uniform for every node: the engine is "closed to
    /// writing, open to reading". The view's structure is auto-tracked (an
    /// <see cref="Effect"/> reading it re-runs on any membership change) and it
    /// narrates insertions/removals exactly, so it is a ready <c>BindItemsFrom</c>
    /// source — the machinery the editor's hierarchy panel reads. Writing stays
    /// closed: a composition is mutated only through the owner's own slots/lists.
    /// </summary>
    public ReadOnlyNodeList Children => _childrenView ??= new ReadOnlyNodeList(this);

    /// <summary>Raw children for the internal views — no auto-tracking. Outside the node, read <see cref="Children"/>.</summary>
    internal IReadOnlyList<Node> ChildrenRaw => _children;

    /// <summary>The structure signal of the children list, tracked by the <see cref="Children"/> view's reads.</summary>
    internal SourceObservers ChildrenStructure => _childrenStructure;

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
        var index = _children.Count - 1;
        child.Parent = this;
        child.OnParentChanged(null, this);
        if (Tree is not null)
            child.EnterTree(Tree);
        NarrateInserted(index, child); // once fully attached, so observers see a coherent tree
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
        var index = _children.IndexOf(child);
        _children.RemoveAt(index);
        child.Parent = null;
        child.OnParentChanged(this, null);
        NarrateRemoved(index, child);
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
        var oldIndex = oldParent?._children.IndexOf(child) ?? -1;
        oldParent?._children.RemoveAt(oldIndex);
        _children.Add(child);
        var newIndex = _children.Count - 1;
        child.Parent = this;
        child.OnParentChanged(oldParent, this);
        oldParent?.NarrateRemoved(oldIndex, child);
        if (!sameTree && Tree is not null)
            child.EnterTree(Tree);
        NarrateInserted(newIndex, child); // once fully attached, so observers see a coherent tree
    }

    /// <summary>Narrates a child's arrival (indexed) and invalidates the children structure signal.</summary>
    private void NarrateInserted(int index, Node child)
    {
        ChildInserted?.Invoke(index, child);
        _childrenStructure.NotifyChanged();
    }

    /// <summary>Narrates a child's departure (indexed) and invalidates the children structure signal.</summary>
    private void NarrateRemoved(int index, Node child)
    {
        ChildDeparted?.Invoke(index, child);
        _childrenStructure.NotifyChanged();
    }

    /// <summary>
    /// Creates a single-child slot owned by this node. Only a node can create
    /// slots for itself, so closed compositions stay closed. Expose the slot
    /// through a public property to open this part of the composition (containers);
    /// keep it private to stay closed (scenes).
    /// </summary>
    protected NodeSlot<TChild> CreateChildSlot<TChild>() where TChild : Node
    {
        var slot = new NodeSlot<TChild>(this);
        ChildDeparted += slot.OnChildDeparted;
        return slot;
    }

    /// <summary>
    /// Creates a typed children collection owned by this node, supporting C#
    /// collection initializers. Same closure rule as <see cref="CreateChildSlot{TChild}"/>.
    /// </summary>
    protected NodeList<TChild> CreateChildList<TChild>() where TChild : Node
    {
        var list = new NodeList<TChild>(this);
        ChildDeparted += list.OnChildDeparted;
        Disposing += list.Unbind;
        return list;
    }

    /// <summary>
    /// Creates a reactive cell whose binding lifetime is tied to this node's:
    /// disposing the node releases the cell's binding deterministically, so a
    /// live source can never keep pushing into — and retaining — a dead subtree.
    /// Only the death severs: a detached node stays alive and keeps synchronizing.
    /// </summary>
    protected Property<T> CreateProperty<T>(T initialValue)
    {
        var cell = new Property<T>(initialValue);
        Disposing += cell.Unbind;
        return cell;
    }

    /// <summary>
    /// Creates an <see cref="Effect"/> tied to this node's lifetime: it runs
    /// <paramref name="action"/> now and again whenever a reactive value it read
    /// changes, and is disposed with the node — a dead subtree stops reacting (and
    /// stops retaining what it read), no manual <c>.Changed</c> bookkeeping.
    /// </summary>
    protected Effect CreateEffect(Action action)
    {
        var effect = new Effect(action);
        Disposing += effect.Dispose;
        return effect;
    }

    /// <summary>
    /// Creates a <see cref="Computed{T}"/> tied to this node's lifetime: the
    /// derived value tracks its formula and stops recomputing when the node dies.
    /// </summary>
    protected Computed<T> CreateComputed<T>(Func<T> formula)
    {
        var computed = new Computed<T>(formula);
        Disposing += computed.Dispose;
        return computed;
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
    /// Publishes <paramref name="value"/> under type <typeparamref name="T"/> for
    /// this node's subtree. Any descendant can retrieve it with <see cref="Inherit{T}"/>.
    /// Calling this more than once overwrites the previous value. The provider is
    /// this node itself — descendants start their search from their parent.
    /// </summary>
    public void Provide<T>(T value) where T : class
    {
        _provided ??= new Dictionary<Type, object>();
        _provided[typeof(T)] = value;
    }

    /// <summary>
    /// Walks up the ancestor chain and returns the nearest value published under
    /// <typeparamref name="T"/> via <see cref="Provide{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No ancestor provides <typeparamref name="T"/>.</exception>
    public T Inherit<T>() where T : class
    {
        if (TryInherit<T>(out var value))
            return value!;
        throw new InvalidOperationException(
            $"No ancestor provides {typeof(T).Name}. Call Provide<{typeof(T).Name}>() on an ancestor first.");
    }

    /// <summary>
    /// Tries to walk up the ancestor chain for a value published under
    /// <typeparamref name="T"/>. Returns false when no ancestor provides it.
    /// </summary>
    public bool TryInherit<T>(out T? value) where T : class
    {
        for (var node = Parent; node is not null; node = node.Parent)
        {
            if (node._provided is not null && node._provided.TryGetValue(typeof(T), out var obj))
            {
                value = (T)obj;
                return true;
            }
        }
        value = null;
        return false;
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
    /// Detaches from the parent beforehand, and releases every binding held by
    /// the node's cells and containers — bindings live exactly as long as their
    /// owner. Idempotent.
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
        Disposing?.Invoke();
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

    /// <summary>
    /// Recursively draws the living subtree, parents before children (painter's
    /// order). No snapshot: structural mutation during a draw is a bug — fail
    /// fast — and the legitimate need, destruction, goes through
    /// <see cref="QueueDispose"/>, flushed at end of frame.
    /// </summary>
    internal void RenderSubtree(IRenderer renderer)
    {
        (this as VisualNode)?.Draw(renderer);
        foreach (var child in _children)
            child.RenderSubtree(renderer);
    }

    /// <summary>
    /// Finds the topmost <see cref="UINode"/> containing the position — the
    /// render traversal, reversed: last children first (they drew last, they
    /// are on top), depth first. No clipping, consistent with rendering:
    /// children may overflow their parent and still be hit.
    /// </summary>
    internal UINode? HitTest(Vector2 position)
    {
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var hit = _children[i].HitTest(position);
            if (hit is not null)
                return hit;
        }
        return this is UINode { Hittable: true } ui && ui.GlobalRect.Contains(position) ? ui : null;
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