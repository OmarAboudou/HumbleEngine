namespace HumbleEngine;

/// <summary>
/// A read-only view of a reactive value, returned by
/// <see cref="Property{T}.AsReadOnly"/>. Unlike exposing the cell typed as
/// <see cref="IReadOnlyProperty{T}"/> — which a cast defeats — this is a distinct
/// type with no write path and no way back to the cell, so read-only is enforced
/// by the type (the reasoning behind the BCL's <c>ReadOnlyCollection</c>).
/// <para>
/// A thin forwarder: <see cref="Value"/> reads the source (so auto-tracking
/// subscribes to the <i>source</i>, transparently), <see cref="Changed"/> passes
/// subscriptions through. The view is therefore <b>not</b> itself a tracking
/// source — it has no graph identity, it delegates — which is exactly why
/// <see cref="IReadOnlyProperty{T}"/> does not (and should not) imply being one.
/// </para>
/// </summary>
internal sealed class ReadOnlyProperty<T> : IReadOnlyProperty<T>
{
    private readonly IReadOnlyProperty<T> _source;

    internal ReadOnlyProperty(IReadOnlyProperty<T> source) => _source = source;

    /// <inheritdoc />
    public T Value => _source.Value;

    /// <inheritdoc />
    public event Action<T>? Changed
    {
        add    => _source.Changed += value;
        remove => _source.Changed -= value;
    }
}
