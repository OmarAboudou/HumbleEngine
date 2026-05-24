namespace HumbleEngine;

public sealed class BlackBoard
{
    private readonly Dictionary<Type, object> _slots = new();

    public void Set<T>(T value) where T : class => _slots[typeof(T)] = value;

    public T? Get<T>() where T : class
        => _slots.TryGetValue(typeof(T), out var v) ? (T)v : null;
}
