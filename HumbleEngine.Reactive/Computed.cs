namespace HumbleEngine;

/// <summary>
/// A derived value: a formula over other reactive values, recomputed
/// automatically when any of them changes — <c>Computed = read-only Property +
/// automatic dependency wiring</c>. It reads as an <see cref="IReadOnlyProperty{T}"/>
/// (so a <c>Label</c> binds to it, an <see cref="Effect"/> reads it), and it is
/// itself a source: downstream computations that read it re-run when its value
/// changes. The formula auto-tracks its reads — never list the dependencies.
/// <para>
/// Eager and synchronous, like the rest of the graph: it recomputes on a
/// dependency change and notifies only when the value actually changed. Diamonds
/// may see transient intermediates; the settled value is always correct. Owns a
/// subscription — dispose it (or tie it to a node's lifetime). The formula must be
/// pure: read reactive values, do not write them.
/// </para>
/// </summary>
public sealed class Computed<T> : IReadOnlyProperty<T>, IDisposable
{
    private readonly Func<T> _formula;
    private readonly Computation _computation;
    private readonly SourceObservers _observers = new();
    private T _value = default!;
    private bool _hasValue;

    /// <summary>Declares the formula and computes its first value now, tracking what it reads.</summary>
    public Computed(Func<T> formula)
    {
        ArgumentNullException.ThrowIfNull(formula);
        _formula = formula;
        _computation = new Computation(Recompute);
        _computation.Run();
    }

    /// <inheritdoc />
    public event Action<T>? Changed;

    /// <inheritdoc />
    public T Value
    {
        get
        {
            // Reading inside a running Effect/Computed subscribes it to this derived value.
            _observers.Track();
            return _value;
        }
    }

    private void Recompute()
    {
        var next = _formula();
        if (_hasValue && EqualityComparer<T>.Default.Equals(_value, next))
            return;

        var hadValue = _hasValue;
        _value = next;
        _hasValue = true;

        // The first computation is the initial value, not a change — no one is
        // listening at construction anyway; later recomputes notify.
        if (hadValue)
        {
            Changed?.Invoke(next);
            _observers.NotifyChanged();
        }
    }

    /// <inheritdoc />
    Type IObservableValue.ValueType => typeof(T);

    /// <inheritdoc cref="IObservableValue.Value"/>
    object? IObservableValue.Value => Value;

    /// <summary>Stops recomputing and unsubscribes from the formula's dependencies. Idempotent.</summary>
    public void Dispose() => _computation.Dispose();
}
