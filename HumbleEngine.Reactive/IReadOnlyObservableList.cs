namespace HumbleEngine;

/// <summary>
/// Read-only view of a <see cref="ObservableList{T}"/>: indexed access plus exact
/// change narration, no mutation. The natural type for exposing an observable
/// collection in a contract and for <c>BindItemsFrom</c> sources — a mapping only
/// ever reads its source.
/// <para>
/// There is deliberately no "reset" event: every mutation narrates itself
/// exactly, so consumers never need a full-rebuild path.
/// </para>
/// </summary>
public interface IReadOnlyObservableList<T> : IReadOnlyList<T>
{
    /// <summary>
    /// Raised after an item was inserted: it now sits at the given index.
    /// Notifications are synchronous and single-threaded.
    /// </summary>
    event Action<int, T>? Added;

    /// <summary>
    /// Raised after an item left the given index. The item is no longer in the
    /// list — it travels in the event, the index says where it used to be.
    /// </summary>
    event Action<int, T>? Removed;
}
