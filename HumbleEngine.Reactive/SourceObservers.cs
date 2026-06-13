namespace HumbleEngine;

/// <summary>
/// The public, composable primitive for making a type a first-class reactive
/// source — the same machinery <see cref="Property{T}"/> and <see cref="Computed{T}"/>
/// run on, exposed so anything else can join the tracking graph without the wiring
/// (<c>Computation</c>, the tracking context) leaking out. Hold one per observable
/// dimension, call <see cref="Track"/> on every read that depends on it and
/// <see cref="NotifyChanged"/> when it changes. A cell holds one for its value; an
/// observable collection holds one for its structure.
/// <para>
/// It <b>is</b> the reactive source: reads subscribe to <i>it</i>, so the same
/// instance must live as long as the dimension it represents. Single-threaded by
/// design, like the rest of the graph. The observer set is allocated lazily — a
/// source no one reads inside a computation costs nothing beyond this object.
/// </para>
/// </summary>
public sealed class SourceObservers : IReactiveSource
{
    private HashSet<Computation>? _observers;

    /// <summary>
    /// Subscribes the computation running right now (if any) to this source: a
    /// later <see cref="NotifyChanged"/> will re-run it. Call from every read that
    /// depends on the observed dimension; a plain read outside any computation
    /// tracks nothing.
    /// </summary>
    public void Track() => Tracking.Track(this);

    /// <summary>
    /// Re-runs every computation that read this source. The set is copied first: a
    /// re-run re-tracks and so mutates the very set being iterated.
    /// </summary>
    public void NotifyChanged()
    {
        if (_observers is null || _observers.Count == 0)
            return;
        foreach (var observer in _observers.ToArray())
            observer.Invalidate();
    }

    void IReactiveSource.AddObserver(Computation observer) =>
        (_observers ??= new HashSet<Computation>(ReferenceEqualityComparer.Instance)).Add(observer);

    void IReactiveSource.RemoveObserver(Computation observer) => _observers?.Remove(observer);
}
