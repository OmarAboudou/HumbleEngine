using System.Collections;

namespace HumbleEngine;

/// <summary>
/// Typed children collection: the multi-child counterpart of
/// <see cref="NodeSlot{TChild}"/>. Implements <see cref="IEnumerable{T}"/> with a
/// public <see cref="Add"/>, so C# collection initializers work —
/// <c>new Panel { Children = { a, b } }</c> reads like the tree it builds.
/// <para>
/// Adding adopts (ownership transfer, from wherever the node sits); removing
/// detaches (the node stays alive). Members detached or disposed behind the
/// list's back are pruned on the next access. Only the owner can create its own
/// lists (<see cref="Node.CreateChildList{TChild}"/>), so closed compositions
/// stay closed.
/// </para>
/// </summary>
public sealed class NodeList<TChild> : IEnumerable<TChild> where TChild : Node
{
    private readonly Node _owner;
    private readonly List<TChild> _items = [];

    internal NodeList(Node owner) => _owner = owner;

    /// <summary>Number of members currently attached.</summary>
    public int Count
    {
        get
        {
            Prune();
            return _items.Count;
        }
    }

    /// <summary>Member at the given position, in add order.</summary>
    public TChild this[int index]
    {
        get
        {
            Prune();
            return _items[index];
        }
    }

    /// <summary>Adopts a node (from wherever it sits) as the last member.</summary>
    /// <exception cref="InvalidOperationException">The node is already in this list,
    /// or attached to the owner through another slot or list.</exception>
    public void Add(TChild item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Prune();
        if (_items.Contains(item))
            throw new InvalidOperationException($"{item} is already in this list.");
        if (ReferenceEquals(item.Parent, _owner))
            throw new InvalidOperationException(
                $"{item} is already attached to {_owner} through another slot.");

        _owner.Adopt(item);
        _items.Add(item);
    }

    /// <summary>
    /// Detaches a member; it stays alive, owned by whoever references it.
    /// Returns false when the node is not a member.
    /// </summary>
    public bool Remove(TChild item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Prune();
        if (!_items.Remove(item))
            return false;
        _owner.Detach(item);
        return true;
    }

    /// <summary>Detaches every member; they all stay alive.</summary>
    public void Clear()
    {
        Prune();
        foreach (var item in _items)
            _owner.Detach(item);
        _items.Clear();
    }

    /// <inheritdoc />
    public IEnumerator<TChild> GetEnumerator()
    {
        Prune();
        return ((IEnumerable<TChild>)_items.ToArray()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Drops members that were detached or disposed behind the list's back.</summary>
    private void Prune() => _items.RemoveAll(item => !ReferenceEquals(item.Parent, _owner));
}
