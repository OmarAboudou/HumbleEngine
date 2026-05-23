namespace HumbleEngine;

public class Property<T> : IReadOnlyProperty<T>
{
    private Signal<T> _valueChanged = new();
    public ReadOnlySignal<T> ValueChanged => _valueChanged.AsReadOnly();
    
    public Property(T initialValue = default) => _value = initialValue;
    
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if(EqualityComparer<T>.Default.Equals(_value, value))
                return;
            
            _value = value;
            _valueChanged.Emit(_value);               
        }
    }

    private ReadOnlyProperty<T>? _readOnlyProperty;
    public ReadOnlyProperty<T> AsReadOnly() => _readOnlyProperty ??= new(this);

}

public class ReadOnlyProperty<T> : IReadOnlyProperty<T>
{
    private readonly Property<T> _property;

    internal ReadOnlyProperty(Property<T> property) => _property = property;
    
    public ReadOnlySignal<T> ValueChanged => _property.ValueChanged;

    public T Value => _property.Value;
}

public interface IReadOnlyProperty<T>
{
    public ReadOnlySignal<T> ValueChanged { get; }
    public T Value { get; }

}