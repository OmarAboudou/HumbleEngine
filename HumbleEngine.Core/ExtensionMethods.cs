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

    /*#region Emitting

    public static void EmitSignal(this object owner, Signal<Action> signal)
    {
        if(EnsureEmittingIsAllowed(owner, signal)) signal.Connections.ForEach(connection => connection.Delegate.Invoke());
    }

    public static void EmitSignal<T1>(this object owner, Signal<Action<T1>> signal, T1 arg1)
    {
        if(EnsureEmittingIsAllowed(owner, signal)) signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1));
    }
    
    public static void EmitSignal<T1, T2>(this object owner, Signal<Action<T1, T2>> signal, T1 arg1, T2 arg2)
    {
        if(EnsureEmittingIsAllowed(owner, signal)) signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2));
    }
    
    public static void EmitSignal<T1, T2, T3>(this object owner, Signal<Action<T1, T2, T3>> signal, T1 arg1, T2 arg2, T3 arg3)
    {
        if(EnsureEmittingIsAllowed(owner, signal)) signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2, arg3));
    }
    
    public static void EmitSignal<T1, T2, T3, T4>(this object owner, Signal<Action<T1, T2, T3, T4>> signal, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if(EnsureEmittingIsAllowed(owner, signal)) signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2, arg3, arg4));
    }
    
    #endregion*/

    /*
    #region Life Cycle

    public static SignalConnection<TDelegate> ConnectToSignal<TDelegate>(
        this object target,
        Signal<TDelegate> signal,
        TDelegate @delegate
    )
        where TDelegate : Delegate
        => signal.Connect(target, @delegate);

    public static void DisconnectFromSignal<TDelegate>(this object target, Signal<TDelegate> signal, SignalConnection<TDelegate> connection)
        where  TDelegate : Delegate
    => signal.Disconnect(target, connection);

    #endregion
    */

    #endregion
}