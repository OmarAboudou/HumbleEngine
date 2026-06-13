namespace HumbleEngine;

/// <summary>
/// Read-only view of a <see cref="Property{T}"/> cell: current value plus change
/// notification, no write access. The natural type for exposing observable state
/// in a contract — the outside can watch, only the owner can write — and for
/// binding sources, since a binding only ever reads its source.
/// <para>
/// Extends <see cref="IObservableValue{T}"/> so that any cell is inspectable: its
/// value is readable without knowing <c>T</c> (boxed as <c>object?</c>), and an
/// <see cref="Effect"/> reading it through the non-generic interface still
/// auto-tracks (the concrete getter calls <see cref="SourceObservers.Track"/>).
/// </para>
/// </summary>
public interface IReadOnlyProperty<T> : IObservableValue<T>
{
    /// <summary>
    /// Raised after the value actually changed, with the new value.
    /// Notifications are synchronous and single-threaded.
    /// </summary>
    event Action<T>? Changed;
}
