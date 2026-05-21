namespace HumbleEngine;

public class ReactiveProperty<T>
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
            foreach (var listener in _listeners.ToArray())
                listener(value);
        }
    }

    public void Subscribe(Action<T> listener) => _listeners.Add(listener);

    public void Unsubscribe(Action<T> listener) => _listeners.Remove(listener);

    public void BindFrom(ReactiveProperty<T> source)
    {
        source.Subscribe(v => Value = v);
        Value = source.Value;
    }
}
