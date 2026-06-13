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
/// <para>
/// A slot is an observable cell (<see cref="IReadOnlyProperty{T}"/>) — "observable
/// + tree semantics". It narrates every occupancy change <b>at the moment it
/// happens</b>: a replacement is a departure (<see cref="Changed"/> with null)
/// followed by an arrival, and an occupant leaving behind the slot's back —
/// adopted elsewhere or disposed — narrates its departure immediately.
/// </para>
/// </summary>
public sealed class NodeSlot<TChild> : IReadOnlyProperty<TChild?>
    where TChild : Node
{
    private readonly Node _owner;
    private TChild? _value;

    internal NodeSlot(Node owner) => _owner = owner;

    /// <summary>Raised after the occupant changed: the new occupant, or null on departure.</summary>
    public event Action<TChild?>? Changed;

    /// <summary>
    /// Current occupant, or null. Setting adopts the new occupant (from wherever
    /// it sits — tree hooks fire if its liveness changes) and detaches the
    /// previous one, which stays alive.
    /// </summary>
    /// <exception cref="InvalidOperationException">The node is already attached to
    /// the owner through another slot or list.</exception>
    public TChild? Value
    {
        get => _value;
        set
        {
            if (ReferenceEquals(_value, value))
                return;
            if (value is not null && ReferenceEquals(value.Parent, _owner))
                throw new InvalidOperationException(
                    $"{value} is already attached to {_owner} through another slot.");

            if (_value is not null)
                _owner.Detach(_value); // the departure broadcast empties the slot and narrates null
            if (value is not null)
            {
                _owner.Adopt(value);
                _value = value;
                Changed?.Invoke(value);
            }
        }
    }

    /// <inheritdoc />
    Type IObservableValue.ValueType => typeof(TChild);

    /// <inheritdoc cref="IObservableValue.Value"/>
    object? IObservableValue.Value => Value;

    /// <summary>
    /// Owner broadcast (<see cref="Node.ChildDeparted"/>): a child of the owner
    /// just left it — when it was the occupant, the slot empties and narrates. The
    /// index (the child's position among all the owner's children) is irrelevant to
    /// a single-child slot.
    /// </summary>
    internal void OnChildDeparted(int _, Node child)
    {
        if (!ReferenceEquals(_value, child))
            return;
        _value = null;
        Changed?.Invoke(null);
    }
}
