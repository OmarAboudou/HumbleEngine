namespace HumbleEngine.Core;

public interface IPropertyGetter<T>
{
    public T Value { get; }
    
    public delegate void NewValueHandler(T newValue);
    
    public void Connect(NewValueHandler handler);
    
    public void Disconnect(NewValueHandler handler);
    
    
    public delegate void ValueChangedHandler(T oldValue, T newValue);

    public void Connect(ValueChangedHandler handler);
    
    public void Disconnect(ValueChangedHandler handler);
    
}

public class Property<T> : IPropertyGetter<T>
{
    public Property(T initialValue)
    {
        _value = initialValue;
    }

    public static implicit operator T(Property<T> property) => property.Value;
    
    public IPropertyGetter<T> Getter => field ??= new PropertyGetter<T>(this);
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                T oldValue = _value;
                _value = value;
                NewValueEvent?.Invoke(oldValue);
                ValueChangedEvent?.Invoke(oldValue, value);
            }
        }
    }

    private event IPropertyGetter<T>.NewValueHandler? NewValueEvent;
    public void Connect(IPropertyGetter<T>.NewValueHandler handler)
        => NewValueEvent += handler;

    public void Disconnect(IPropertyGetter<T>.NewValueHandler handler)
        => NewValueEvent -= handler;

    private event IPropertyGetter<T>.ValueChangedHandler? ValueChangedEvent;
    public void Connect(IPropertyGetter<T>.ValueChangedHandler handler) 
        => ValueChangedEvent += handler;

    public void Disconnect(IPropertyGetter<T>.ValueChangedHandler handler) 
        => ValueChangedEvent -= handler;
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

    public void Connect(IPropertyGetter<T>.NewValueHandler handler)
        => _property.Connect(handler);

    public void Disconnect(IPropertyGetter<T>.NewValueHandler handler)
        => _property.Connect(handler);

    public void Connect(IPropertyGetter<T>.ValueChangedHandler handler)
        =>  _property.Connect(handler);

    public void Disconnect(IPropertyGetter<T>.ValueChangedHandler handler)
        =>  _property.Disconnect(handler);
}