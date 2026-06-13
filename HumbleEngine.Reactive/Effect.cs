namespace HumbleEngine;

/// <summary>
/// A side-effect that re-runs whenever a reactive value it <b>reads</b> changes —
/// the sink of the reactive graph, the bridge to the imperative world (sync a
/// window title, autosave, relayout). The action runs once immediately, tracking
/// which <see cref="Property{T}"/> (and future Computed) it reads; from then on
/// it re-runs on any of their changes, re-tracking each time — never list the
/// dependencies, the reads list themselves.
/// <para>
/// Owns a subscription: dispose it to stop (tie it to a node's lifetime, the way
/// a property is tied through <c>CreateProperty</c>). Writing a value the effect
/// also reads is the one footgun — it is rejected like any re-entrant write.
/// </para>
/// </summary>
public sealed class Effect : IDisposable
{
    private readonly Computation _computation;

    /// <summary>Runs <paramref name="action"/> now, tracking its reads, and again whenever they change.</summary>
    public Effect(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _computation = new Computation(action);
        _computation.Run();
    }

    /// <summary>Stops the effect and unsubscribes from everything it read. Idempotent.</summary>
    public void Dispose() => _computation.Dispose();
}
