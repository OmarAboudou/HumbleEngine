namespace HumbleEngine.Core;

public interface IProperty<T>
{
    public T Value { get; }
    
    public void Connect(Action<T> callback);
    public void Disconnect(Action<T> callback);

    public void BindFrom(IProperty<T> other) 
        => BindFrom(other, IdentityFunction);

    public void Bind2Way(IProperty<T> other)
        => Bind2Way(other, IdentityFunction, IdentityFunction);
    
    public void BindFrom<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformation);

    public void Bind2Way<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        this.BindFrom(other, transformationFrom);
        other.BindFrom(this, transformationTo);
    }
    
}

public class EditableProperty<T> : IProperty<T>, IDisposable
{
    internal EditableProperty(T initialValue)
    {
        _value = initialValue;
    }
    
    public static implicit operator T(EditableProperty<T> editableProperty) => editableProperty.Value;
    
    public IProperty<T> Getter => field ??= new Property<T>(this);
    private T _value;
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                Emit(_value);
            }
        }
    }

    private readonly List<WeakReference<Action<T>>> _othersListeningToMe = [];
    private readonly List<Delegate> _meListeningToOthers = [];
    
    public void Connect(Action<T> callback)
    {
        CleanConnections();
        
        _othersListeningToMe.Add(new WeakReference<Action<T>>(callback));
    }

    public void Disconnect(Action<T> callback)
    {
        CleanConnections();
        
        _othersListeningToMe.RemoveAll(wr =>
        {
            wr.TryGetTarget(out var target);
            return target == callback || target == null;
        });
    }

    public void Emit(T value)
    {
        CleanConnections();
        
        _othersListeningToMe.ForEach(c =>
        {
            if (c.TryGetTarget(out var target)) 
                target?.Invoke(value);
        });
    }
    
    private void CleanConnections()
        => _othersListeningToMe.RemoveAll(wr => !wr.TryGetTarget(out _));
    
    public void BindFrom<TDest>(IProperty<TDest> other, Func<TDest, T> transformation)
    {
        Action<TDest> lambda = otherValue => Value = transformation(otherValue); 
        other.Connect(lambda);
        this._meListeningToOthers.Add(lambda);
        this.Value = transformation(other.Value);
    }


    public void Dispose()
    {
        _othersListeningToMe.Clear();
        _meListeningToOthers.Clear();
    }
}

public class Property<T> : IProperty<T>
{
    internal Property(EditableProperty<T> editableProperty)
    {
        _editableProperty = editableProperty;
    }

    public static implicit operator T(Property<T> property) => property.Value;
    
    private readonly EditableProperty<T> _editableProperty;

    public T Value => _editableProperty.Value;

    public void Connect(Action<T> callback) 
        => _editableProperty.Connect(callback);

    public void Disconnect(Action<T> callback) 
        => _editableProperty.Disconnect(callback);

    public void BindFrom<TDest>(IProperty<TDest> other, Func<TDest, T> transformation) 
        => _editableProperty.BindFrom(other, transformation);

}