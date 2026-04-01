namespace HumbleEngine.Core;

/// <summary>
/// General-purpose extension methods used across the engine.
/// </summary>
public static class ExtensionMethods
{
    #region Collections

    /// <summary>
    /// Executes <paramref name="action"/> on each element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
    /// <param name="source">The sequence to iterate over.</param>
    /// <param name="action">The action to execute on each element.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action){
        foreach (T item in source) action(item);
    }

    #endregion

    #region Signals

    #region Creation

    /// <summary>Creates a signal with no parameters owned by <paramref name="owner"/>.</summary>
    /// <param name="owner">The object that owns and may emit this signal.</param>
    /// <param name="name">The name of the signal, used in logging and tooling.</param>
    public static EmittableSignal CreateSignal(this object owner, string name)
        => new(new(owner, name));

    /// <inheritdoc cref="CreateSignal(object, string)"/>
    /// <typeparam name="T1">The type of the first parameter.</typeparam>
    /// <param name="arg1Name">The display name of the first parameter.</param>
    public static EmittableSignal<T1> CreateSignal<T1>(this object owner, string name, string arg1Name)
        => new(new(owner, name, (typeof(T1), arg1Name)));

    /// <inheritdoc cref="CreateSignal{T1}(object, string, string)"/>
    /// <typeparam name="T2">The type of the second parameter.</typeparam>
    /// <param name="arg2Name">The display name of the second parameter.</param>
    public static EmittableSignal<T1, T2> CreateSignal<T1, T2>(this object owner, string name, string arg1Name, string arg2Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name)));

    /// <inheritdoc cref="CreateSignal{T1, T2}(object, string, string, string)"/>
    /// <typeparam name="T3">The type of the third parameter.</typeparam>
    /// <param name="arg3Name">The display name of the third parameter.</param>
    public static EmittableSignal<T1, T2, T3> CreateSignal<T1, T2, T3>(this object owner, string name, string arg1Name, string arg2Name, string arg3Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name)));

    /// <inheritdoc cref="CreateSignal{T1, T2, T3}(object, string, string, string, string)"/>
    /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
    /// <param name="arg4Name">The display name of the fourth parameter.</param>
    public static EmittableSignal<T1, T2, T3, T4> CreateSignal<T1, T2, T3, T4>(this object owner, string name, string arg1Name, string arg2Name, string arg3Name, string arg4Name)
        => new(new(owner, name, (typeof(T1), arg1Name), (typeof(T2), arg2Name), (typeof(T3), arg3Name), (typeof(T4), arg4Name)));

    #endregion

    #endregion
}
