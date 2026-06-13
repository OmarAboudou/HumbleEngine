namespace HumbleEngine;

/// <summary>
/// A value observable by the inspector: its runtime type, its current value
/// (untyped for heterogeneous lists), with the auto-tracking guarantee — the
/// concrete getter calls <see cref="SourceObservers.Track"/> so an
/// <see cref="Effect"/> reading <see cref="Value"/> re-runs whenever the value
/// changes, even through the non-generic interface.
/// <para>
/// Use <c>is IObservableValue&lt;T&gt;</c> to recover the typed value or
/// <see cref="IReadOnlyProperty{T}"/> to also subscribe to the change event.
/// Any type that implements <see cref="IObservableValue{T}"/> becomes inspectable
/// without further ceremony.
/// </para>
/// </summary>
public interface IObservableValue
{
    /// <summary>Runtime type of the value — the inspector uses this to choose a widget.</summary>
    Type ValueType { get; }

    /// <summary>
    /// Current value, boxed. Delegates to the concrete tracked getter — an
    /// <see cref="Effect"/> reading this subscribes to the underlying source and
    /// re-runs on change.
    /// </summary>
    object? Value { get; }
}

/// <summary>
/// Typed, covariant view of an <see cref="IObservableValue"/>: <see cref="Value"/>
/// hides the boxed root with the concrete type. Prefer this form when <c>T</c> is
/// known at the call site; the non-generic root is for heterogeneous collections
/// (e.g. the inspector's property list).
/// </summary>
public interface IObservableValue<out T> : IObservableValue
{
    /// <summary>Current value, typed. Tracked: reads inside an <see cref="Effect"/> subscribe.</summary>
    new T Value { get; }
}
