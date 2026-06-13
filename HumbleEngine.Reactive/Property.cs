namespace HumbleEngine;

/// <summary>
/// A reactive cell: a current value plus change notification — the spreadsheet
/// cell of the engine. Write <see cref="Value"/> directly, or declare a formula
/// once with <see cref="BindFrom{TSource}(IReadOnlyProperty{TSource}, Func{TSource, T})"/>
/// and the system maintains it from then on.
/// <para>
/// A cell holds at most one binding — one formula — but can be listened to by any
/// number of others. Notifications are synchronous, single-threaded, and only fire
/// when the value actually changes (<see cref="EqualityComparer{T}.Default"/>).
/// Transformations must be pure: diamond-shaped graphs may observe transient
/// intermediate states, but the final value is always correct.
/// </para>
/// </summary>
public sealed class Property<T> : IReadOnlyProperty<T>, IReactiveCell
{
    private T _value;
    private IBinding? _binding;
    private bool _notifying;
    private readonly SourceObservers _observers = new();
    private ReadOnlyProperty<T>? _readOnly;

    /// <summary>Creates a free cell holding the given initial value.</summary>
    public Property(T initialValue) => _value = initialValue;

    /// <inheritdoc />
    public event Action<T>? Changed;

    /// <summary>Whether this cell is currently an end of a binding.</summary>
    public bool IsBound => _binding is not null;

    /// <summary>
    /// A read-only view of this cell — readable and observable, with no write path
    /// and no cast back to the cell. Expose this (not the cell typed as an
    /// interface) when the outside must only watch. The view is created once and
    /// reused: the same reference every call.
    /// </summary>
    public IReadOnlyProperty<T> AsReadOnly() => _readOnly ??= new ReadOnlyProperty<T>(this);

    /// <summary>
    /// Current value. Setting notifies subscribers only when the value actually
    /// changes. Both ends of a two-way binding stay writable — user input enters
    /// there and propagates to the other end.
    /// </summary>
    /// <exception cref="InvalidOperationException">The cell is the target of a
    /// one-way binding — the binding is its only legitimate writer; call
    /// <see cref="Unbind"/> first to take back manual control.</exception>
    public T Value
    {
        get
        {
            // Reading inside a running Effect/Computed subscribes it; a plain read tracks nothing.
            _observers.Track();
            return _value;
        }
        set
        {
            if (_binding is { IsTwoWay: false })
                throw new InvalidOperationException(
                    "This cell is the target of a one-way binding, which is its only legitimate writer. Call Unbind() first.");
            SetValueCore(value);
        }
    }

    /// <summary>
    /// Declares this cell's formula: it mirrors <paramref name="source"/> through
    /// <paramref name="transform"/>, starting now — the source's current value is
    /// pushed immediately — and until <see cref="Unbind"/> or a replacing bind.
    /// Re-binding over a one-way binding replaces it; the source is never affected
    /// by being listened to.
    /// </summary>
    /// <exception cref="InvalidOperationException">The binding would create a
    /// cycle (this cell is already upstream of the source), or this cell is an
    /// end of a two-way binding — replacing it silently would break the partner
    /// cell's contract, unbind explicitly first.</exception>
    public void BindFrom<TSource>(IReadOnlyProperty<TSource> source, Func<TSource, T> transform)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(transform);
        if (_binding is { IsTwoWay: true })
            throw new InvalidOperationException(
                "This cell is an end of a two-way binding; call Unbind() before re-binding.");
        ThrowIfCycle(source);

        _binding?.Dispose();
        _binding = new OneWayBinding<TSource, T>(source, this, transform);
    }

    /// <summary>Identity overload: this cell mirrors the source as-is.</summary>
    /// <inheritdoc cref="BindFrom{TSource}(IReadOnlyProperty{TSource}, Func{TSource, T})"/>
    public void BindFrom(IReadOnlyProperty<T> source) => BindFrom(source, static v => v);

    /// <summary>
    /// Declares an explicit bidirectional contract with <paramref name="other"/> —
    /// never two crossed one-way bindings. <paramref name="other"/> is the source
    /// of truth at bind time: this cell takes its value immediately; afterwards
    /// the relation is symmetric. Echo is suppressed: a change on either end makes
    /// exactly one trip, so termination never depends on the transformations being
    /// perfect inverses. The binding occupies the slot of <b>both</b> cells.
    /// </summary>
    /// <param name="other">The partner cell, source of truth for the initial sync.</param>
    /// <param name="fromOther">Transformation applied when the partner's value flows here.</param>
    /// <param name="toOther">Transformation applied when this cell's value flows to the partner.</param>
    /// <exception cref="InvalidOperationException">Either cell already holds a
    /// binding (both slots must be free), or <paramref name="other"/> is this cell.</exception>
    public void BindTwoWayFrom<TOther>(
        Property<TOther> other, Func<TOther, T> fromOther, Func<T, TOther> toOther)
    {
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(fromOther);
        ArgumentNullException.ThrowIfNull(toOther);
        if (ReferenceEquals(other, this))
            throw new InvalidOperationException("A cell cannot be two-way bound to itself.");
        if (IsBound || other.IsBound)
            throw new InvalidOperationException(
                "A two-way binding occupies the binding slot of both cells; both must be unbound. Call Unbind() first.");

        var binding = new TwoWayBinding<T, TOther>(this, other, toOther, fromOther);
        _binding = binding.EndA;
        other._binding = binding.EndB;
        binding.SyncInitialFromB();
    }

    /// <summary>Identity overload: both cells mirror each other as-is.</summary>
    /// <inheritdoc cref="BindTwoWayFrom{TOther}(Property{TOther}, Func{TOther, T}, Func{T, TOther})"/>
    public void BindTwoWayFrom(Property<T> other) =>
        BindTwoWayFrom(other, static v => v, static v => v);

    /// <summary>
    /// Releases this cell's binding deterministically; the cell keeps its current
    /// value and becomes freely writable again. For a two-way binding, both ends
    /// are released. No-op when unbound.
    /// </summary>
    public void Unbind()
    {
        var binding = _binding;
        _binding = null;
        binding?.Dispose();
    }

    /// <summary>
    /// Writes bypassing the manual-write guard — the path bindings use. Notifies
    /// only on actual change; a re-entrant diverging write (a handler writing
    /// back into the cell it is notified by) fails fast instead of overflowing.
    /// </summary>
    internal void SetValueCore(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value))
            return;
        if (_notifying)
            throw new InvalidOperationException(
                "Re-entrant write: a Changed handler wrote a diverging value back into the cell that notified it.");

        _value = value;
        _notifying = true;
        try
        {
            Changed?.Invoke(value);
            _observers.NotifyChanged();
        }
        finally
        {
            _notifying = false;
        }
    }

    /// <summary>Clears the binding slot without disposing — called by the binding itself.</summary>
    internal void ClearBinding() => _binding = null;

    /// <inheritdoc />
    Type IObservableValue.ValueType => typeof(T);

    /// <inheritdoc cref="IObservableValue.Value"/>
    object? IObservableValue.Value => Value;

    object? IReactiveCell.BindingSource => _binding?.Source;

    /// <summary>
    /// Walks the chain of binding sources upstream of <paramref name="source"/> and
    /// fails fast if this cell appears in it. Each cell has at most one binding, so
    /// the chain is linear; the visited set terminates on two-way pairs, which are
    /// legitimate internal 2-cycles and dead ends for value provenance.
    /// </summary>
    private void ThrowIfCycle(object source)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        object? current = source;
        while (current is not null && visited.Add(current))
        {
            if (ReferenceEquals(current, this))
                throw new InvalidOperationException(
                    "This binding would create a cycle: the target cell is already upstream of the source. Use BindTwoWayFrom for a mutual relation.");
            current = (current as IReactiveCell)?.BindingSource;
        }
    }
}
