namespace HumbleEngine.Core;

/// <summary>
/// General-purpose extension methods used across the engine.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Executes <paramref name="action"/> on each element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The sequence to iterate over.</param>
    /// <param name="action">The action to execute on each element.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action){
        foreach (T item in source) action(item);
    }

    #region Signals

    #region Creation
    
    public static Signal CreateSignal(this object owner, string name) 
        => new(owner, name);

    public static Signal<T1> CreateSignal<T1>(this object owner, string name, string? arg1Name)
        => new(owner, name, (typeof(T1), arg1Name));
    
    public static Signal<T1, T2> CreateSignal<T1, T2>(this object owner, string name, string? arg1Name, string? arg2Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name));

    public static Signal<T1, T2, T3> CreateSignal<T1, T2, T3>(this object owner, string name, string? arg1Name, string? arg2Name, string? arg3Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name));

    public static Signal<T1, T2, T3, T4> CreateSignal<T1, T2, T3, T4>(this object owner, string name, string? arg1Name, string? arg2Name, string? arg3Name, string? arg4Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name), (typeof(T4), arg4Name));
    
    #endregion

    #region Emitting

    public static void Emit(this object owner, Signal signal)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ForEach(conn => conn.Delegate.Invoke());
    }

    public static void Emit<T1>(this object owner, Signal<T1> signal, T1 arg1)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ForEach(conn => conn.Delegate.Invoke(arg1));
    }

    public static void Emit<T1, T2>(this object owner, Signal<T1, T2> signal, T1 arg1, T2 arg2)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ForEach(conn => conn.Delegate.Invoke(arg1, arg2));
    }

    public static void Emit<T1, T2, T3>(this object owner, Signal<T1, T2, T3> signal, T1 arg1, T2 arg2, T3 arg3)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ForEach(conn => conn.Delegate.Invoke(arg1, arg2, arg3));
    }

    public static void Emit<T1, T2, T3, T4>(this object owner, Signal<T1, T2, T3, T4> signal, T1 arg1, T2 arg2,
        T3 arg3, T4 arg4)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ForEach(conn => conn.Delegate.Invoke(arg1, arg2, arg3, arg4));
    }
    
    #endregion

    private static void ValidateEmittingPermission<TDelegate, TSelf>(object owner, SignalBase<TDelegate, TSelf> signal)
        where TDelegate : Delegate 
        where TSelf : SignalBase<TDelegate, TSelf>
    {
        if (signal.Owner != owner) throw new InvalidOperationException($"Only the owner of a signal can emit it.");
    }
    
    #endregion
}