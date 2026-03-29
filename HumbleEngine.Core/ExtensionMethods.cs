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
        foreach (T item in source)
        {
            action(item);
        }
    }
}