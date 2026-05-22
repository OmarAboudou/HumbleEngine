namespace HumbleEngine;

public interface ISignal
{
    void Connect(Action listener);
    void Disconnect(Action listener);
}

public interface ISignal<out T>
{
    void Connect(Action<T> listener);
    void Disconnect(Action<T> listener);
}

public sealed class MutableSignal : ISignal
{
    private readonly List<Action> _listeners = new();

    public Signal Signal { get; }

    public MutableSignal() => Signal = new Signal(this);

    public void Emit()
    {
        foreach (var l in _listeners.ToArray()) l();
    }

    public void Connect(Action listener)    => _listeners.Add(listener);
    public void Disconnect(Action listener) => _listeners.Remove(listener);
}

public sealed class Signal : ISignal
{
    private readonly MutableSignal _owner;

    internal Signal(MutableSignal owner) => _owner = owner;

    public void Connect(Action listener)    => _owner.Connect(listener);
    public void Disconnect(Action listener) => _owner.Disconnect(listener);
}

public sealed class MutableSignal<T> : ISignal<T>
{
    private readonly List<Action<T>> _listeners = new();

    public Signal<T> Signal { get; }

    public MutableSignal() => Signal = new Signal<T>(this);

    public void Emit(T value)
    {
        foreach (var l in _listeners.ToArray()) l(value);
    }

    public void Connect(Action<T> listener)    => _listeners.Add(listener);
    public void Disconnect(Action<T> listener) => _listeners.Remove(listener);
}

public sealed class Signal<T> : ISignal<T>
{
    private readonly MutableSignal<T> _owner;

    internal Signal(MutableSignal<T> owner) => _owner = owner;

    public void Connect(Action<T> listener)    => _owner.Connect(listener);
    public void Disconnect(Action<T> listener) => _owner.Disconnect(listener);
}
