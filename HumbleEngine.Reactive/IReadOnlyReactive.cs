namespace HumbleEngine;

/// <summary>
/// Read-only view of a <see cref="Reactive{T}"/> cell: current value plus change
/// notification, no write access. The natural type for exposing observable state
/// in a contract — the outside can watch, only the owner can write — and for
/// binding sources, since a binding only ever reads its source.
/// </summary>
public interface IReadOnlyReactive<T>
{
    /// <summary>Current value, readable at any time.</summary>
    T Value { get; }

    /// <summary>
    /// Raised after the value actually changed, with the new value.
    /// Notifications are synchronous and single-threaded.
    /// </summary>
    event Action<T>? Changed;
}
