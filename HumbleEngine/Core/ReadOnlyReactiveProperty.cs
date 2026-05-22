namespace HumbleEngine;

public class ReadOnlyReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    private readonly ReactiveProperty<T> _reactiveProperty;
    
    public ReadOnlyReactiveProperty(ReactiveProperty<T> reactiveProperty)
        => _reactiveProperty = reactiveProperty;

    public T Value => _reactiveProperty.Value;

    public void Connect(Action<T> listener)
    {
        _reactiveProperty.Connect(listener);
    }

    public void Disconnect(Action<T> listener)
    {
        _reactiveProperty.Disconnect(listener);
    }
}