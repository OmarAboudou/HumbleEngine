using System.Collections;

namespace HumbleEngine;

/// <summary>
/// Typed children collection: the multi-child counterpart of
/// <see cref="NodeSlot{TChild}"/>. Implements <see cref="IEnumerable{T}"/> with a
/// public <see cref="Add"/>, so C# collection initializers work —
/// <c>new Panel { Children = { a, b } }</c> reads like the tree it builds.
/// Adding adopts (ownership transfer, from wherever the node sits); removing
/// detaches (the node stays alive). Only the owner can create its own lists
/// (<see cref="Node.CreateChildList{TChild}"/>), so closed compositions stay closed.
/// <para>
/// The list is observable (<see cref="IReadOnlyReactiveList{T}"/>) — "observable
/// + tree semantics". Every membership change narrates itself <b>at the moment it
/// happens</b>, including members leaving behind the list's back (adopted
/// elsewhere, disposed): <see cref="Count"/> is exact at all times.
/// </para>
/// <para>
/// <see cref="BindItemsFrom{TItem}"/> declares the list as the positional mirror of a
/// data list: the binding becomes its only legitimate writer — the mapping
/// creates a node per item and disposes it with its item ("the tree owns").
/// </para>
/// </summary>
public sealed class NodeList<TChild> : IReadOnlyReactiveList<TChild>
    where TChild : Node
{
    private readonly Node _owner;
    private readonly List<TChild> _items = [];
    private IDisposable? _binding;
    private bool _bindingWriting;

    internal NodeList(Node owner) => _owner = owner;

    /// <inheritdoc />
    public event Action<int, TChild>? Added;

    /// <inheritdoc />
    public event Action<int, TChild>? Removed;

    /// <summary>Number of members currently attached — exact at all times.</summary>
    public int Count => _items.Count;

    /// <summary>Member at the given position, in add order.</summary>
    public TChild this[int index] => _items[index];

    /// <summary>Whether this list is currently driven by a <see cref="BindItemsFrom{TItem}"/> mapping.</summary>
    public bool IsBound => _binding is not null;

    /// <summary>Adopts a node (from wherever it sits) as the last member.</summary>
    /// <exception cref="InvalidOperationException">The node is already in this list,
    /// already attached to the owner through another slot or list, or the list is
    /// bound — the mapping is its only legitimate writer; call <see cref="Unbind"/>
    /// first.</exception>
    public void Add(TChild item)
    {
        ThrowIfBound();
        Insert(_items.Count, item);
    }

    /// <summary>
    /// Detaches a member; it stays alive, owned by whoever references it.
    /// Returns false when the node is not a member.
    /// </summary>
    /// <exception cref="InvalidOperationException">The list is bound — call
    /// <see cref="Unbind"/> first.</exception>
    public bool Remove(TChild item)
    {
        ThrowIfBound();
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.Contains(item))
            return false;
        _owner.Detach(item); // the departure broadcast updates the list and narrates
        return true;
    }

    /// <summary>Detaches every member, from the end towards the start; they all stay alive.</summary>
    /// <exception cref="InvalidOperationException">The list is bound — call
    /// <see cref="Unbind"/> first.</exception>
    public void Clear()
    {
        ThrowIfBound();
        for (var i = _items.Count - 1; i >= 0; i--)
            _owner.Detach(_items[i]);
    }

    /// <summary>
    /// Declares this list as the positional mirror of <paramref name="source"/>:
    /// <c>this[i]</c> is the node fabricated for <c>source[i]</c>, the index is the
    /// registry. The current content is mirrored immediately; from then on every
    /// source insertion fabricates and adopts a node, and every source removal
    /// <b>disposes</b> the mirrored node — the factory created it for the binding,
    /// the binding owns it (a reference never confers ownership). Call
    /// <see cref="Unbind"/> first to take the nodes back instead.
    /// </summary>
    /// <exception cref="InvalidOperationException">The list is already bound, or
    /// not empty — a positional mirror starts from a clean slate.</exception>
    public void BindItemsFrom<TItem>(IReadOnlyReactiveList<TItem> source, Func<TItem, TChild> factory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(factory);
        if (_binding is not null)
            throw new InvalidOperationException(
                "This list is already bound; call Unbind() before re-binding.");
        if (_items.Count > 0)
            throw new InvalidOperationException(
                "BindItemsFrom requires an empty list: the mirror is positional and starts from a clean slate.");

        _binding = new ItemsBinding<TItem>(this, source, factory);
    }

    /// <summary>
    /// Releases the <see cref="BindItemsFrom{TItem}"/> mapping deterministically. The
    /// mirrored nodes stay in place — they are yours now — and the list becomes
    /// freely writable again. No-op when unbound.
    /// </summary>
    public void Unbind()
    {
        var binding = _binding;
        _binding = null;
        binding?.Dispose();
    }

    /// <inheritdoc />
    public IEnumerator<TChild> GetEnumerator() =>
        ((IEnumerable<TChild>)_items.ToArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Owner broadcast (<see cref="Node.ChildDeparted"/>): a child of the owner
    /// just left it — when it was a member, the list updates and narrates, at the
    /// moment it happens.
    /// </summary>
    internal void OnChildDeparted(Node child)
    {
        if (child is not TChild typed)
            return;
        var index = _items.IndexOf(typed);
        if (index < 0)
            return;
        if (IsBound && !_bindingWriting)
            throw new InvalidOperationException(
                $"{typed} belongs to a bound list but left it outside the mapping (disposed or adopted manually?) — the positional mirror would desynchronize. Call Unbind() first.");

        _items.RemoveAt(index);
        Removed?.Invoke(index, typed);
    }

    /// <summary>Adopts and inserts at an exact position — the shared path of <see cref="Add"/> and the mapping.</summary>
    private void Insert(int index, TChild item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.Contains(item))
            throw new InvalidOperationException($"{item} is already in this list.");
        if (ReferenceEquals(item.Parent, _owner))
            throw new InvalidOperationException(
                $"{item} is already attached to {_owner} through another slot.");

        _owner.Adopt(item);
        _items.Insert(index, item);
        Added?.Invoke(index, item);
    }

    private void ThrowIfBound()
    {
        if (_binding is not null)
            throw new InvalidOperationException(
                "This list is bound: the mapping is its only legitimate writer. Call Unbind() first.");
    }

    /// <summary>
    /// The live <see cref="BindItemsFrom{TItem}"/> mapping: subscribes to the source's
    /// exact narration and maintains the positional mirror — fabricate and adopt
    /// on insertion, dispose on removal. Disposing only unsubscribes; the nodes
    /// stay with the list.
    /// </summary>
    private sealed class ItemsBinding<TItem> : IDisposable
    {
        private readonly NodeList<TChild> _target;
        private readonly IReadOnlyReactiveList<TItem> _source;
        private readonly Func<TItem, TChild> _factory;

        internal ItemsBinding(
            NodeList<TChild> target, IReadOnlyReactiveList<TItem> source, Func<TItem, TChild> factory)
        {
            _target = target;
            _source = source;
            _factory = factory;
            source.Added += OnSourceAdded;
            source.Removed += OnSourceRemoved;
            for (var i = 0; i < source.Count; i++)
                OnSourceAdded(i, source[i]);
        }

        public void Dispose()
        {
            _source.Added -= OnSourceAdded;
            _source.Removed -= OnSourceRemoved;
        }

        private void OnSourceAdded(int index, TItem item)
        {
            var node = _factory(item);
            _target._bindingWriting = true;
            try
            {
                _target.Insert(index, node);
            }
            finally
            {
                _target._bindingWriting = false;
            }
        }

        private void OnSourceRemoved(int index, TItem item)
        {
            var node = _target._items[index];
            _target._bindingWriting = true;
            try
            {
                node.Dispose(); // detaching broadcasts back: the list updates and narrates
            }
            finally
            {
                _target._bindingWriting = false;
            }
        }
    }
}
