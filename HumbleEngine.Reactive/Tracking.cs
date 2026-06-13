namespace HumbleEngine;

/// <summary>
/// Something a <see cref="Computation"/> can depend on — a <see cref="Property{T}"/>
/// (and, later, a <c>Computed</c>). Reading it inside a running computation
/// subscribes that computation; a change invalidates every subscriber.
/// </summary>
internal interface IReactiveSource
{
    void AddObserver(Computation observer);
    void RemoveObserver(Computation observer);
}

/// <summary>
/// The auto-tracking context: which computation is currently running. A reactive
/// source's getter calls <see cref="Track"/>, registering itself as a dependency
/// of that computation — so an <see cref="Effect"/> never lists what it reads,
/// the reads list themselves. Single-threaded by design (thread-static current).
/// </summary>
internal static class Tracking
{
    [ThreadStatic] private static Computation? _current;

    /// <summary>The computation running right now, or null outside any (a plain read tracks nothing).</summary>
    internal static Computation? Current
    {
        get => _current;
        set => _current = value;
    }

    /// <summary>Records that the running computation (if any) read <paramref name="source"/>.</summary>
    internal static void Track(IReactiveSource source) => _current?.AddDependency(source);

    /// <summary>
    /// Runs <paramref name="fn"/> with no current computation, so the reactive
    /// reads inside it subscribe nothing — the engine behind <c>Reactive.Untrack</c>.
    /// Restores the previous context afterwards (re-entrant inside a computation).
    /// </summary>
    internal static T RunUntracked<T>(Func<T> fn)
    {
        var previous = _current;
        _current = null;
        try
        {
            return fn();
        }
        finally
        {
            _current = previous;
        }
    }
}

/// <summary>
/// One re-runnable computation behind an <see cref="Effect"/> (and a future
/// Computed): it runs an action while tracking the reactive sources it reads,
/// subscribes to them, and re-runs when any of them changes — re-tracking each
/// time, so dependencies can come and go between runs.
/// </summary>
internal sealed class Computation
{
    private readonly Action _run;
    private readonly HashSet<IReactiveSource> _dependencies = new(ReferenceEqualityComparer.Instance);
    private bool _running;
    private bool _disposed;

    internal Computation(Action run) => _run = run;

    /// <summary>
    /// Drops the previous dependencies, runs the action with this computation as
    /// the tracking context (every reactive read re-subscribes), restores the
    /// previous context.
    /// </summary>
    internal void Run()
    {
        if (_disposed)
            return;

        Unsubscribe();
        var previous = Tracking.Current;
        Tracking.Current = this;
        _running = true;
        try
        {
            _run();
        }
        finally
        {
            _running = false;
            Tracking.Current = previous;
        }
    }

    /// <summary>Called by <see cref="Tracking.Track"/> for each source read during a run.</summary>
    internal void AddDependency(IReactiveSource source)
    {
        if (_dependencies.Add(source))
            source.AddObserver(this);
    }

    /// <summary>A dependency changed — re-run, unless we are already running (no synchronous self-recursion).</summary>
    internal void Invalidate()
    {
        if (_disposed || _running)
            return;
        Run();
    }

    /// <summary>Stops the computation for good and unsubscribes from every dependency.</summary>
    internal void Dispose()
    {
        _disposed = true;
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        foreach (var dependency in _dependencies)
            dependency.RemoveObserver(this);
        _dependencies.Clear();
    }
}
