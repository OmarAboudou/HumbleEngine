namespace HumbleEngine.Core;

public interface IPropertyGetter<T>
{
    public T Value { get; }
    
    public delegate void ValueChangedHandler(T value);
    public Signal<ValueChangedHandler> ValueChanged { get; }
    
}

public class Property<T> : IPropertyGetter<T>
{
    public Property(T initialValue)
    {
        _value = initialValue;
        ValueChanged = new Signal<IPropertyGetter<T>.ValueChangedHandler>(c => c(_value), out _valueChangedEmitter);
    }
    
    public static implicit operator T(Property<T> property) => property.Value;
    
    public Signal<IPropertyGetter<T>.ValueChangedHandler> ValueChanged { get; }

    private Action _valueChangedEmitter;
    
    private T _value;
    
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                _valueChangedEmitter();
            }
        }
    }

    public IPropertyGetter<T> Getter => field ??= new PropertyGetter<T>(this);
    
}

public class PropertyGetter<T> : IPropertyGetter<T>
{
    internal PropertyGetter(Property<T> property)
    {
        _property = property;
    }

    public static implicit operator T(PropertyGetter<T> property) => property.Value;
    
    private readonly Property<T> _property;

    public T Value => _property.Value;

    public Signal<IPropertyGetter<T>.ValueChangedHandler> ValueChanged
        => _property.ValueChanged;

}