using System.Collections;

namespace HumbleEngine;

/// <summary>
/// An observable list that narrates its mutations exactly — "added X at 2",
/// never "something changed". Two events suffice for every operation:
/// <see cref="Clear"/> is N removals from the end, replacing through the
/// indexer is a removal followed by an insertion at the same index. Consumers
/// (decoration, <c>BindItemsFrom</c> mirrors) only ever write the incremental path.
/// <para>
/// Duplicates are allowed — this is data, not nodes. Notifications are
/// synchronous and single-threaded; between the two events of a replacement
/// the list is in a consistent intermediate state (without the old item).
/// Structural mutation from inside a handler of the same list fails fast.
/// </para>
/// </summary>
public sealed class ObservableList<T> : IReadOnlyObservableList<T>
{
    private readonly List<T> _items = [];
    private readonly SourceObservers _structure = new();
    private bool _notifying;

    /// <inheritdoc />
    public event Action<int, T>? Added;

    /// <inheritdoc />
    public event Action<int, T>? Removed;

    /// <summary>
    /// Number of items currently in the list. Reading it inside an
    /// <see cref="Effect"/>/<see cref="Computed{T}"/> subscribes to the list's
    /// structure — any later add/remove re-runs the computation.
    /// </summary>
    public int Count
    {
        get
        {
            _structure.Track();
            return _items.Count;
        }
    }

    /// <summary>
    /// Item at the given position. Setting replaces: <see cref="Removed"/> fires
    /// for the old item, then <see cref="Added"/> for the new one, both at this
    /// index — another item means another mirror node. Assigning an equal value
    /// (<see cref="EqualityComparer{T}.Default"/>) does nothing, so mirrors are
    /// never rebuilt for free.
    /// </summary>
    public T this[int index]
    {
        get
        {
            _structure.Track();
            return _items[index];
        }
        set
        {
            var current = _items[index];
            if (EqualityComparer<T>.Default.Equals(current, value))
                return;
            ThrowIfNotifying();

            _items.RemoveAt(index);
            Notify(Removed, index, current);
            _items.Insert(index, value);
            Notify(Added, index, value);
        }
    }

    /// <summary>Appends an item; <see cref="Added"/> fires with its index.</summary>
    public void Add(T item)
    {
        ThrowIfNotifying();
        _items.Add(item);
        Notify(Added, _items.Count - 1, item);
    }

    /// <summary>Inserts an item at the given position; later items shift right.</summary>
    public void Insert(int index, T item)
    {
        ThrowIfNotifying();
        _items.Insert(index, item);
        Notify(Added, index, item);
    }

    /// <summary>
    /// Removes the first occurrence of an item; returns false when absent.
    /// </summary>
    public bool Remove(T item)
    {
        ThrowIfNotifying();
        var index = _items.IndexOf(item);
        if (index < 0)
            return false;
        RemoveAtCore(index);
        return true;
    }

    /// <summary>Removes the item at the given position; later items shift left.</summary>
    public void RemoveAt(int index)
    {
        ThrowIfNotifying();
        _ = _items[index];
        RemoveAtCore(index);
    }

    /// <summary>
    /// Empties the list as N removals from the end towards the start — indices
    /// stay valid throughout the teardown, and no reset event ever exists.
    /// </summary>
    public void Clear()
    {
        ThrowIfNotifying();
        for (var i = _items.Count - 1; i >= 0; i--)
            RemoveAtCore(i);
    }

    /// <summary>Whether the item occurs at least once.</summary>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>Index of the first occurrence, or -1.</summary>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        _structure.Track();
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void RemoveAtCore(int index)
    {
        var item = _items[index];
        _items.RemoveAt(index);
        Notify(Removed, index, item);
    }

    private void Notify(Action<int, T>? handler, int index, T item)
    {
        _notifying = true;
        try
        {
            // Exact narration first (BindItemsFrom, decoration), then the structure
            // signal (the layout-style computations that re-read the whole list) —
            // both inside the notifying window, so either path mutating the list
            // fails fast against the indices being narrated.
            handler?.Invoke(index, item);
            _structure.NotifyChanged();
        }
        finally
        {
            _notifying = false;
        }
    }

    private void ThrowIfNotifying()
    {
        if (_notifying)
            throw new InvalidOperationException(
                "The list is notifying a change; mutating it from inside one of its own handlers would invalidate the indices being narrated.");
    }
}
