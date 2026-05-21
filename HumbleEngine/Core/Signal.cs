namespace HumbleEngine;

public sealed class Signal
{
    private readonly List<Action> _listeners = new();

    private Signal() { }

    public static (Signal Signal, Action Emit) Create()
    {
        var s = new Signal();
        return (s, s.EmitCore);
    }

    private void EmitCore()
    {
        foreach (var l in _listeners.ToArray()) l();
    }

    public void Connect(Action listener) => _listeners.Add(listener);

    public void Disconnect(Action listener) => _listeners.Remove(listener);
}

public sealed class Signal<T>
{
    private readonly List<Action<T>> _listeners = new();

    private Signal() { }

    public static (Signal<T> Signal, Action<T> Emit) Create()
    {
        var s = new Signal<T>();
        return (s, s.EmitCore);
    }

    private void EmitCore(T value)
    {
        foreach (var l in _listeners.ToArray()) l(value);
    }

    public void Connect(Action<T> listener) => _listeners.Add(listener);

    public void Disconnect(Action<T> listener) => _listeners.Remove(listener);
}
