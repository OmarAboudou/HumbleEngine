namespace HumbleEngine;

/// <summary>
/// Boundary of the living tree: nodes whose ancestor chain reaches this tree's
/// <see cref="Root"/> are "in tree" and receive the tree lifecycle hooks. Also
/// hosts the deferred-dispose queue behind <see cref="Node.QueueDispose"/>.
/// <para>
/// The tree knows nothing about windows — wiring it to a surface and a frame
/// loop is the application's business, one tree per driven surface. Rendering
/// passes through <see cref="Render"/>: the renderer is handed in per call,
/// never retained. There is no global tree: the application owns the lifecycle,
/// as everywhere else in the engine.
/// </para>
/// </summary>
public sealed class SceneTree : IDisposable
{
    private readonly List<Node> _disposeQueue = [];
    private Node? _root;

    /// <summary>True once <see cref="Dispose"/> has run.</summary>
    public bool IsDisposed { get; private set; }

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
    /// Draws the living tree: walks the root subtree, parents before children
    /// (painter's order), letting every <see cref="VisualNode"/> submit its
    /// draws. Call between the renderer's <see cref="IRenderer.BeginFrame"/>
    /// and <see cref="IRenderer.EndFrame"/>. The renderer is not retained —
    /// the tree holds no rendering state between frames.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This tree is disposed.</exception>
    public void Render(IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _root?.RenderSubtree(renderer);
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
        IsDisposed = true;
    }

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
