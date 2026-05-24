namespace HumbleEngine.Core;

public interface IPropertyGetter<T>
{
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
                ValueChanged?.Invoke(oldValue, value);
            }
        }
    }

    public event IPropertyGetter<T>.ValueChangedHandler? ValueChanged;
    public void Connect(IPropertyGetter<T>.ValueChangedHandler handler)
    {
        ValueChanged += handler;
    }

    public void Disconnect(IPropertyGetter<T>.ValueChangedHandler handler)
    {
        ValueChanged -= handler;
    }
}

internal class PropertyGetter<T> : IPropertyGetter<T>
{
    internal PropertyGetter(Property<T> property)
    {
        _property = property;
    }

    private readonly Property<T> _property;

    public T Value => _property.Value;

    public void Connect(IPropertyGetter<T>.ValueChangedHandler handler)
        =>  _property.Connect(handler);

    public void Disconnect(IPropertyGetter<T>.ValueChangedHandler handler)
        =>  _property.Disconnect(handler);
}