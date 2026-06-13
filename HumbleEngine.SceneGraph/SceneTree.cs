namespace HumbleEngine;

/// <summary>
/// Boundary of the living tree: nodes whose ancestor chain reaches this tree's
/// <see cref="Root"/> are "in tree" and receive the tree lifecycle hooks. Also
/// hosts the deferred-dispose queue behind <see cref="Node.QueueDispose"/>.
/// <para>
/// A tree is the third of the trio "one window ↔ one renderer ↔ one tree": the
/// application injects the window's renderer at construction, and the tree
/// hands it to nodes through the framework protocol
/// (<see cref="VisualNode.Renderer"/>) — nodes acquire GPU resources when
/// entering the tree, release them when leaving. The tree still knows nothing
/// about windows: wiring the frame loop is the application's business, and
/// multi-window means several trios side by side. There is no global tree: the
/// application owns the lifecycle, as everywhere else in the engine.
/// (The in-tree WindowNode/viewport model is the noted evolution — see roadmap 09.)
/// </para>
/// </summary>
public sealed class SceneTree : IDisposable
{
    private const int DefaultFontPixelSize = 18;

    private readonly List<Node> _disposeQueue = [];
    private readonly InputRouter _inputRouter = new();
    private Node? _root;
    private Font? _defaultFont;
    private GlyphAtlas? _defaultFontAtlas;

    /// <summary>
    /// Renderer driving this tree's surface — injected by the application,
    /// read by nodes through <see cref="VisualNode.Renderer"/>.
    /// </summary>
    public IRenderer Renderer { get; }

    /// <summary>Creates a tree paired with the renderer of the surface it drives.</summary>
    public SceneTree(IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Renderer = renderer;
    }

    /// <summary>True once <see cref="Dispose"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// The tree's shared default glyph atlas (DejaVu Sans at a UI size), created
    /// lazily from <see cref="Renderer"/> on first use and owned by the tree:
    /// every <c>Label</c> of this tree draws through it, the way they all draw
    /// through the tree's renderer — what lets a Label be default-constructible
    /// (its font comes from the context, not its constructor). Disposed with the
    /// tree. A reactive font size and a per-size cache are the noted evolution.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This tree is disposed.</exception>
    public GlyphAtlas DefaultFontAtlas
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (_defaultFontAtlas is null)
            {
                _defaultFont = Font.Default(DefaultFontPixelSize);
                _defaultFontAtlas = new GlyphAtlas(Renderer, _defaultFont);
            }
            return _defaultFontAtlas;
        }
    }

    /// <summary>
    /// Root node of the living tree. Setting it makes the new root's whole subtree
    /// enter the tree (hooks fire); replacing or clearing it makes the previous
    /// root's subtree exit. The previous root stays alive, owned by whoever holds
    /// its reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The new root has a parent, or is
    /// already the root of another tree.</exception>
    /// <exception cref="ObjectDisposedException">This tree or the new root is disposed.</exception>
    public Node? Root
    {
        get => _root;
        set
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (ReferenceEquals(_root, value))
                return;
            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (value.Parent is not null)
                    throw new InvalidOperationException($"{value} has a parent — only a detached node can become a root.");
                if (value.IsTreeRoot)
                    throw new InvalidOperationException($"{value} is already the root of another SceneTree.");
            }

            var previous = _root;
            _root = null;
            previous?.ExitTree();
            _root = value;
            value?.EnterTree(this);
        }
    }

    /// <summary>
    /// Draws the living tree with <see cref="Renderer"/>: walks the root
    /// subtree, parents before children (painter's order), letting every
    /// <see cref="VisualNode"/> submit its draws. Call between the renderer's
    /// <see cref="IRenderer.BeginFrame"/> and <see cref="IRenderer.EndFrame"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This tree is disposed.</exception>
    public void Render()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _root?.RenderSubtree(Renderer);

        // Hover is derived state (pointer × geometry): the geometry half just
        // settled for this frame — re-evaluate, so a node sliding under a
        // still pointer gains/loses hover like one moved onto it.
        _inputRouter.RefreshHover(_root);
    }

    /// <summary>
    /// Routes one input event into the living tree: positional events are
    /// hit-tested (reverse painter's order) and bubble from the target up the
    /// <see cref="UINode"/> ancestors until consumed
    /// (<see cref="UINode"/>.<c>OnInput</c> returning <c>true</c>); hover
    /// enter/exit are synthesized per node; a press captures the pointer until
    /// its release. The application wires it next to rendering:
    /// <c>window.OnInput += tree.RouteInput</c>.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This tree is disposed.</exception>
    public void RouteInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _inputRouter.Route(_root, inputEvent);
    }

    /// <summary>
    /// Disposes every node queued by <see cref="Node.QueueDispose"/>, including
    /// nodes queued while flushing. This is the "safe point" of the deferred
    /// destruction — the application calls it at the end of each frame iteration,
    /// after presenting, when no hook or handler is in flight: a node queued during
    /// frame N still renders in N and is gone before N+1.
    /// </summary>
    public void FlushDisposeQueue()
    {
        while (_disposeQueue.Count > 0)
        {
            var batch = _disposeQueue.ToArray();
            _disposeQueue.Clear();
            foreach (var node in batch)
                node.Dispose();
        }
    }

    /// <summary>
    /// Disposes the root subtree (exit hooks fire first), flushes the dispose
    /// queue, and makes the tree unusable. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;
        _root?.Dispose();
        FlushDisposeQueue();
        _defaultFontAtlas?.Dispose();
        _defaultFont?.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    /// Node holding the keyboard focus, or null — key and text events route to
    /// it first, then bubble (see <see cref="UINode.GrabFocus"/>).
    /// </summary>
    public UINode? FocusedNode => _inputRouter.Focused;

    /// <summary>Moves the keyboard focus (router state — dead nodes resolve to null).</summary>
    internal void SetFocus(UINode? node) => _inputRouter.Focus(node);

    /// <summary>Registers a node for the next <see cref="FlushDisposeQueue"/>.</summary>
    internal void EnqueueDispose(Node node) => _disposeQueue.Add(node);

    /// <summary>
    /// Called by a root node disposing itself: exits its subtree from the tree
    /// and clears <see cref="Root"/>.
    /// </summary>
    internal void DetachRoot()
    {
        var previous = _root;
        _root = null;
        previous?.ExitTree();
    }
}
