namespace HumbleEngine.Core;

public class Property<T>
{
    public Property(T initialValue)
    {
        _value = initialValue;
    }
    
    public delegate void ValueChangedHandler(T oldValue, T newValue);
    public event ValueChangedHandler? ValueChanged;
    
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
    

    
}