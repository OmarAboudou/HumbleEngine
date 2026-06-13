using System.Collections;

namespace HumbleEngine;

/// <summary>
/// Public, read-only observable view of a node's real children
/// (<see cref="Node.Children"/>). Read-only is guaranteed <b>by the type</b> — no
/// write path, no cast back to a mutable list (the reasoning behind the BCL's
/// <c>ReadOnlyCollection</c>) — so a node's interior is open to reading, closed to
/// writing. The structure is auto-tracked (a read inside an <see cref="Effect"/> /
/// <see cref="Computed{T}"/> subscribes it) and membership changes narrate exactly
/// (<see cref="Added"/> / <see cref="Removed"/>, with index), so the view is a ready
/// <c>BindItemsFrom</c> source — what the editor's hierarchy panel reads.
/// <para>
/// A thin forwarder over the owner's children: it holds no list of its own, so it
/// can never desynchronize. Created once per node and reused.
/// </para>
/// </summary>
public sealed class ReadOnlyNodeList : IReadOnlyObservableList<Node>
{
    private readonly Node _owner;

    internal ReadOnlyNodeList(Node owner)
    {
        _owner = owner;
        _owner.ChildInserted += OnInserted;
        _owner.ChildDeparted += OnRemoved;
    }

    /// <inheritdoc />
    public event Action<int, Node>? Added;

    /// <inheritdoc />
    public event Action<int, Node>? Removed;

    /// <summary>
    /// Number of children. Reading it inside an <see cref="Effect"/> /
    /// <see cref="Computed{T}"/> subscribes to the children structure — any later
    /// membership change re-runs the computation.
    /// </summary>
    public int Count
    {
        get
        {
            _owner.ChildrenStructure.Track();
            return _owner.ChildrenRaw.Count;
        }
    }

    /// <summary>Child at the given position, in attach order. Reading subscribes the running computation to the structure.</summary>
    public Node this[int index]
    {
        get
        {
            _owner.ChildrenStructure.Track();
            return _owner.ChildrenRaw[index];
        }
    }

    /// <inheritdoc />
    public IEnumerator<Node> GetEnumerator()
    {
        _owner.ChildrenStructure.Track();
        // Snapshot: structural mutation while enumerating must not break the walk.
        return ((IEnumerable<Node>)_owner.ChildrenRaw.ToArray()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnInserted(int index, Node child) => Added?.Invoke(index, child);

    private void OnRemoved(int index, Node child) => Removed?.Invoke(index, child);
}
