namespace HumbleEngine.Core;

public interface IPropertyGetter<T>
{
    public T Value { get; }
    
    public delegate void ValueChangedHandler(T value);
    
    public Signal<ValueChangedHandler> ValueChanged { get; }

    public void BindFrom(IPropertyGetter<T> property);
    
    public void BindFrom<TDest>(IPropertyGetter<TDest> source, Func<TDest, T> transformation);

}

public class Property<T> : IPropertyGetter<T>
{
    public Property(T initialValue)
    {
        _value = initialValue;
        ValueChanged = new Signal<IPropertyGetter<T>.ValueChangedHandler>(c => c(_value), out EmitValueChanged);
    }
    
    public static implicit operator T(Property<T> property) => property.Value;
    
    public Signal<IPropertyGetter<T>.ValueChangedHandler> ValueChanged { get; }
    
    // ReSharper disable once InconsistentNaming
    private readonly Action EmitValueChanged;
    
    private T _value;
    
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                EmitValueChanged();
            }
        }
    }

    public IPropertyGetter<T> Getter => field ??= new PropertyGetter<T>(this);

    public void Bind2Way(IPropertyGetter<T> property)
    {
        this.BindFrom(property);
        property.BindFrom(this);
    }

    public void Bind2Way<TDest>(
        IPropertyGetter<TDest> source,
        Func<TDest, T> transformationFrom,
        Func<T, TDest> transformationTo)
    {
        this.BindFrom(source, transformationFrom);
        source.BindFrom(this, transformationTo);
    }
    
    public void BindFrom(IPropertyGetter<T> property)
        => BindFrom(property, IdentityFunction);
    
    public void BindFrom<TDest>(
        IPropertyGetter<TDest> source,
        Func<TDest, T> transformation)
    {
        source.ValueChanged.Connect( value => Value = transformation(value) );
        Value = transformation(source.Value);
    }
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

    public void BindFrom(IPropertyGetter<T> property) 
        => _property.BindFrom(property);

    public void BindFrom<TDest>(IPropertyGetter<TDest> source, Func<TDest, T> transformation) 
        => _property.BindFrom(source, transformation);
}