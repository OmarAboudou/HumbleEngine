namespace HumbleEngine;

public class ReactiveProperty<T> : ISignal<T>
{
    private T _value;
    private readonly List<Action<T>> _listeners = new();

    public ReactiveProperty(T initial) => _value = initial;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            foreach (var l in _listeners.ToArray()) l(value);
        }
    }

    public void Connect(Action<T> listener)    => _listeners.Add(listener);
    public void Disconnect(Action<T> listener) => _listeners.Remove(listener);

    public void BindFrom(ReactiveProperty<T> source)
    {
        source.Connect(v => Value = v);
        Value = source.Value;
    }
}
