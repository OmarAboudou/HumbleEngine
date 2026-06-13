namespace Reconciliation;

/// <summary>The app: a parent that can rebuild, containing a Counter below a label.</summary>
public sealed class App : StatefulWidget
{
    public override State CreateState() => new AppState();
}

public sealed class AppState : State
{
    private int _rebuilds;

    /// <summary>Forces the parent to rebuild — the moment that tests state preservation.</summary>
    public void Tick() => SetState(() => _rebuilds++);

    public override Widget Build() => new Column(
        new Text($"parent rebuilds: {_rebuilds}"),
        new Counter()); // a fresh Counter widget every build — yet its State must survive
}

/// <summary>A self-contained counter holding its own count.</summary>
public sealed class Counter : StatefulWidget
{
    public override State CreateState() => new CounterState();
}

public sealed class CounterState : State
{
    private int _count;

    public void Increment() => SetState(() => _count++);

    public override Widget Build() => new Text($"count: {_count}");
}
