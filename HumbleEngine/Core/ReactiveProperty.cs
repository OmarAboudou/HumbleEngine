namespace HumbleEngine;

public class ReactiveProperty<T> : IReactiveProperty<T>
{
    private T _value;
    private readonly MutableSignal<T> _signal = new();
    private readonly MutableSignal<T, T> _reaffected = new();
    // TODO : Add doc to explain that first param is oldValue and second is newValue...
    public ISignal<T, T> Reaffected => _reaffected.Signal;

    public ReactiveProperty(T initial) => _value = initial;
    
    public static implicit operator T(ReactiveProperty<T> value) => value.Value;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) 
                return;
            
            T oldValue = _value;
            _value = value;
            _signal.Emit(value);
            _reaffected.Emit(oldValue, value);
        }
    }

    public void Connect(Action<T> listener)    => _signal.Connect(listener);
    public void Disconnect(Action<T> listener) => _signal.Disconnect(listener);

    public void BindFrom(ReactiveProperty<T> source)
    {
        source.Connect(v => Value = v);
        Value = source.Value;
    }
}
