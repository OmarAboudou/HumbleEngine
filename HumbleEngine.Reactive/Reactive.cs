namespace HumbleEngine;

/// <summary>
/// Free-standing primitives of the reactive graph that belong to no single cell —
/// today the tracking escape hatch <see cref="Untrack{T}(System.Func{T})"/>.
/// </summary>
public static class Reactive
{
    /// <summary>
    /// Reads outside the surrounding computation: runs <paramref name="fn"/> with
    /// the tracking context suspended, so any reactive value read inside does
    /// <b>not</b> subscribe the enclosing <see cref="Effect"/>/<see cref="Computed{T}"/>.
    /// The escape hatch for an effect that, as a side effect, reads sources it must
    /// not depend on (e.g. building widgets that bind to a value the effect should
    /// ignore). Restores the previous context afterwards.
    /// </summary>
    public static T Untrack<T>(Func<T> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        return Tracking.RunUntracked(fn);
    }

    /// <summary>
    /// The void overload of <see cref="Untrack{T}(System.Func{T})"/>: runs
    /// <paramref name="action"/> with tracking suspended.
    /// </summary>
    public static void Untrack(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Tracking.RunUntracked<object?>(() =>
        {
            action();
            return null;
        });
    }
}
