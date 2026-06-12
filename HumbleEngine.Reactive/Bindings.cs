namespace HumbleEngine;

/// <summary>
/// A live binding occupying a cell's single binding slot. Disposing releases the
/// subscription deterministically — for a two-way binding, both ends at once.
/// </summary>
internal interface IBinding : IDisposable
{
    /// <summary>Both ends of a two-way binding stay manually writable; a one-way target does not.</summary>
    bool IsTwoWay { get; }

    /// <summary>The upstream cell, for bind-time cycle detection.</summary>
    object Source { get; }
}

/// <summary>
/// Internal view of a cell for bind-time cycle detection: exposes the source of
/// its current binding so the chain can be walked. Foreign
/// <see cref="IReadOnlyReactive{T}"/> implementations end the walk — a divergence
/// escaping through one is still caught by the re-entrancy guard at runtime.
/// </summary>
internal interface IReactiveCell
{
    /// <summary>Source of the cell's current binding, or null when unbound.</summary>
    object? BindingSource { get; }
}

/// <summary>
/// One-way binding: the target mirrors the source through a pure transformation.
/// Subscribing pushes the source's current value immediately — declaring the
/// formula computes it, like typing it into a spreadsheet cell.
/// </summary>
internal sealed class OneWayBinding<TSource, TTarget> : IBinding
{
    private readonly IReadOnlyReactive<TSource> _source;
    private readonly Reactive<TTarget> _target;
    private readonly Func<TSource, TTarget> _transform;

    internal OneWayBinding(
        IReadOnlyReactive<TSource> source, Reactive<TTarget> target, Func<TSource, TTarget> transform)
    {
        _source = source;
        _target = target;
        _transform = transform;
        source.Changed += OnSourceChanged;
        OnSourceChanged(source.Value);
    }

    public bool IsTwoWay => false;

    public object Source => _source;

    public void Dispose() => _source.Changed -= OnSourceChanged;

    private void OnSourceChanged(TSource value) => _target.SetValueCore(_transform(value));
}

/// <summary>
/// Two-way binding between cells A and B: an explicit bidirectional contract,
/// echo-suppressed — a change on either end propagates exactly once and never
/// bounces back, so termination does not depend on the transformations being
/// perfect inverses. Third parties listening on either cell still see every
/// change. Each cell holds one <see cref="End"/> in its binding slot; releasing
/// either end releases both.
/// </summary>
internal sealed class TwoWayBinding<TA, TB>
{
    private readonly Reactive<TA> _a;
    private readonly Reactive<TB> _b;
    private readonly Func<TA, TB> _aToB;
    private readonly Func<TB, TA> _bToA;
    private bool _propagating;

    internal TwoWayBinding(Reactive<TA> a, Reactive<TB> b, Func<TA, TB> aToB, Func<TB, TA> bToA)
    {
        _a = a;
        _b = b;
        _aToB = aToB;
        _bToA = bToA;
        EndA = new End(this, b);
        EndB = new End(this, a);
        a.Changed += OnAChanged;
        b.Changed += OnBChanged;
    }

    /// <summary>Slot occupant for cell A; its upstream source is B.</summary>
    internal IBinding EndA { get; }

    /// <summary>Slot occupant for cell B; its upstream source is A.</summary>
    internal IBinding EndB { get; }

    /// <summary>Initial sync: A takes B's value — B is the source of truth at bind time.</summary>
    internal void SyncInitialFromB() => PropagateToA(_b.Value);

    private void OnAChanged(TA value)
    {
        if (_propagating)
            return;
        _propagating = true;
        try
        {
            _b.SetValueCore(_aToB(value));
        }
        finally
        {
            _propagating = false;
        }
    }

    private void OnBChanged(TB value)
    {
        if (_propagating)
            return;
        PropagateToA(value);
    }

    private void PropagateToA(TB value)
    {
        _propagating = true;
        try
        {
            _a.SetValueCore(_bToA(value));
        }
        finally
        {
            _propagating = false;
        }
    }

    private void Release()
    {
        _a.Changed -= OnAChanged;
        _b.Changed -= OnBChanged;
        _a.ClearBinding();
        _b.ClearBinding();
    }

    /// <summary>Per-cell slot occupant, delegating release to the shared contract.</summary>
    private sealed class End(TwoWayBinding<TA, TB> owner, object source) : IBinding
    {
        public bool IsTwoWay => true;

        public object Source => source;

        public void Dispose() => owner.Release();
    }
}
