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
    
    public static SignalSource CreateSignal(this object owner, string name) 
        => new(new(owner, name));

    public static SignalSource<T1> CreateSignal<T1>(this object owner, string name, string? arg1Name)
        => new(new(owner, name, (typeof(T1), arg1Name)));
    
    public static SignalSource<T1, T2> CreateSignal<T1, T2>(this object owner, string name, string? arg1Name, string? arg2Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name)));

    public static SignalSource<T1, T2, T3> CreateSignal<T1, T2, T3>(this object owner, string name, string? arg1Name, string? arg2Name, string? arg3Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name)));
    
    public static SignalSource<T1, T2, T3, T4> CreateSignal<T1, T2, T3, T4>(this object owner, string name, string? arg1Name, string? arg2Name, string? arg3Name, string? arg4Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name), (typeof(T4), arg4Name)));
    
    #endregion

    #endregion
}