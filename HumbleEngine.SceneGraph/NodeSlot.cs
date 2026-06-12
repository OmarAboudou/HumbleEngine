namespace HumbleEngine;

/// <summary>
/// Single-child slot: one typed emplacement in a node's composition. Assigning
/// transfers ownership to the owner node — the tree owns — and the previous
/// occupant is detached, returning to whoever holds its reference.
/// <para>
/// Slots implement the scene contract: a node exposes a typed property delegating
/// to a private slot, and the injecting side never sees where the occupant is
/// mounted. Only the owner can create its own slots
/// (<see cref="Node.CreateChildSlot{TChild}"/>), so closed compositions stay closed.
/// </para>
/// </summary>
public sealed class NodeSlot<TChild> where TChild : Node
{
    private readonly Node _owner;
    private TChild? _value;

    internal NodeSlot(Node owner) => _owner = owner;

    /// <summary>
    /// Current occupant, or null. Self-healing: an occupant detached or disposed
    /// behind the slot's back reads as empty. Setting adopts the new occupant
    /// (from wherever it sits — tree hooks fire if its liveness changes) and
    /// detaches the previous one, which stays alive.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is already attached to
    /// the owner through another slot or list.</exception>
    public TChild? Value
    {
        get
        {
            if (_value is not null && !ReferenceEquals(_value.Parent, _owner))
                _value = null;
            return _value;
        }
        set
        {
            var current = Value;
            if (ReferenceEquals(current, value))
                return;
            if (value is not null && ReferenceEquals(value.Parent, _owner))
                throw new InvalidOperationException(
                    $"{value} is already attached to {_owner} through another slot.");

            if (current is not null)
                _owner.Detach(current);
            if (value is not null)
                _owner.Adopt(value);
            _value = value;
        }
    }
}
