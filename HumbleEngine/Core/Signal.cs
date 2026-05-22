namespace HumbleEngine;

public sealed class SignalEmitter
{
    private readonly List<Action> _listeners = new();

    public Signal Signal { get; }

    public SignalEmitter() => Signal = new Signal(this);

    public void Emit()
    {
        foreach (var l in _listeners.ToArray()) l();
    }

    internal void AddListener(Action listener)    => _listeners.Add(listener);
    internal void RemoveListener(Action listener) => _listeners.Remove(listener);
}

public sealed class Signal
{
    private readonly SignalEmitter _emitter;

    internal Signal(SignalEmitter emitter) => _emitter = emitter;

    public void Connect(Action listener)    => _emitter.AddListener(listener);
    public void Disconnect(Action listener) => _emitter.RemoveListener(listener);
}

public sealed class SignalEmitter<T>
{
    private readonly List<Action<T>> _listeners = new();

    public Signal<T> Signal { get; }

    public SignalEmitter() => Signal = new Signal<T>(this);

    public void Emit(T value)
    {
        foreach (var l in _listeners.ToArray()) l(value);
    }

    internal void AddListener(Action<T> listener)    => _listeners.Add(listener);
    internal void RemoveListener(Action<T> listener) => _listeners.Remove(listener);
}

public sealed class Signal<T>
{
    private readonly SignalEmitter<T> _emitter;

    internal Signal(SignalEmitter<T> emitter) => _emitter = emitter;

    public void Connect(Action<T> listener)    => _emitter.AddListener(listener);
    public void Disconnect(Action<T> listener) => _emitter.RemoveListener(listener);
}
