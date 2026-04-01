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

    /// <summary>Creates a signal with no parameters owned by <paramref name="owner"/>.</summary>
    /// <param name="owner">The object that owns and may emit this signal.</param>
    /// <param name="name">The name of the signal, used in logging and tooling.</param>
    public static Signal CreateSignal(this object owner, string name)
        => new(owner, name);

    /// <inheritdoc cref="CreateSignal(object, string)"/>
    /// <typeparam name="T1">The type of the first parameter.</typeparam>
    /// <param name="arg1Name">The display name of the first parameter.</param>
    public static Signal<T1> CreateSignal<T1>(this object owner, string name, string arg1Name)
        => new(owner, name, (typeof(T1), arg1Name));

    /// <inheritdoc cref="CreateSignal{T1}(object, string, string)"/>
    /// <typeparam name="T2">The type of the second parameter.</typeparam>
    /// <param name="arg2Name">The display name of the second parameter.</param>
    public static Signal<T1, T2> CreateSignal<T1, T2>(this object owner, string name, string arg1Name, string arg2Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name));

    /// <inheritdoc cref="CreateSignal{T1, T2}(object, string, string, string)"/>
    /// <typeparam name="T3">The type of the third parameter.</typeparam>
    /// <param name="arg3Name">The display name of the third parameter.</param>
    public static Signal<T1, T2, T3> CreateSignal<T1, T2, T3>(this object owner, string name, string arg1Name, string arg2Name, string arg3Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name));

    /// <inheritdoc cref="CreateSignal{T1, T2, T3}(object, string, string, string, string)"/>
    /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
    /// <param name="arg4Name">The display name of the fourth parameter.</param>
    public static Signal<T1, T2, T3, T4> CreateSignal<T1, T2, T3, T4>(this object owner, string name, string arg1Name, string arg2Name, string arg3Name, string arg4Name)
        => new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name), (typeof(T4), arg4Name));

    #endregion

    #region Emitting

    /// <summary>Emits <paramref name="signal"/>, invoking all connected delegates.</summary>
    /// <param name="owner">Must be the owner of the signal, otherwise an exception is thrown.</param>
    /// <param name="signal">The signal to emit.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="owner"/> is not the signal's owner.</exception>
    public static void Emit(this object owner, Signal signal)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ToHashSet().ForEach(conn => conn.Delegate.Invoke());
    }

    /// <inheritdoc cref="Emit(object, Signal)"/>
    /// <typeparam name="T1">The type of the first parameter.</typeparam>
    /// <param name="arg1">The first argument passed to connected delegates.</param>
    public static void Emit<T1>(this object owner, Signal<T1> signal, T1 arg1)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ToHashSet().ForEach(conn => conn.Delegate.Invoke(arg1));
    }

    /// <inheritdoc cref="Emit{T1}(object, Signal{T1}, T1)"/>
    /// <typeparam name="T2">The type of the second parameter.</typeparam>
    /// <param name="arg2">The second argument passed to connected delegates.</param>
    public static void Emit<T1, T2>(this object owner, Signal<T1, T2> signal, T1 arg1, T2 arg2)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ToHashSet().ForEach(conn => conn.Delegate.Invoke(arg1, arg2));
    }

    /// <inheritdoc cref="Emit{T1, T2}(object, Signal{T1, T2}, T1, T2)"/>
    /// <typeparam name="T3">The type of the third parameter.</typeparam>
    /// <param name="arg3">The third argument passed to connected delegates.</param>
    public static void Emit<T1, T2, T3>(this object owner, Signal<T1, T2, T3> signal, T1 arg1, T2 arg2, T3 arg3)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ToHashSet().ForEach(conn => conn.Delegate.Invoke(arg1, arg2, arg3));
    }

    /// <inheritdoc cref="Emit{T1, T2, T3}(object, Signal{T1, T2, T3}, T1, T2, T3)"/>
    /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
    /// <param name="arg4">The fourth argument passed to connected delegates.</param>
    public static void Emit<T1, T2, T3, T4>(this object owner, Signal<T1, T2, T3, T4> signal, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        ValidateEmittingPermission(owner, signal);
        signal.Connections.ToHashSet().ForEach(conn => conn.Delegate.Invoke(arg1, arg2, arg3, arg4));
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
