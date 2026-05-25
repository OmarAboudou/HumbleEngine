namespace HumbleEngine.Core;

public interface IProperty<T>
{
    public T Value { get; }
    
    public void Connect(Action<T> callback);
    public void Disconnect(Action<T> callback);

    public void BindFrom<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformation);
    public void UnbindFrom<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformation);
    
    public void BindTo<TOther>(
        IProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.BindFrom(this, transformation);
    public void UnbindTo<TOther>(
        IProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.UnbindFrom(this, transformation);

    public void BindFrom(IProperty<T> other) 
        => BindFrom(other, IdentityFunction);
    public void UnbindFrom(IProperty<T> other)
        =>  UnbindFrom(other, IdentityFunction);

    public void BindTo(IProperty<T> other)
        => BindTo(other, IdentityFunction);
    public void UnbindTo(IProperty<T> other)
        => UnbindTo(other, IdentityFunction);

    public void Bind2WayFrom(IProperty<T> other)
        => Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public void Unbind2WayFrom(IProperty<T> other)
        => Unbind2WayFrom(other, IdentityFunction, IdentityFunction);

    public void Bind2WayTo(IProperty<T> other)
        => other.Bind2WayFrom(this);
    public void Unbind2WayTo(IProperty<T> other)
        => other.Unbind2WayFrom(this);
    
    public void Bind2WayFrom<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        this.BindFrom(other, transformationFrom);
        other.BindFrom(this, transformationTo);
    }
    public void Unbind2WayFrom<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(this, transformationTo);
        this.UnbindFrom(other, transformationFrom);
    }

    public void Bind2WayTo<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo) 
        => other.Bind2WayFrom(this, transformationTo, transformationFrom);
    public void Unbind2WayTo<TOther>(
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(this, transformationTo, transformationFrom);
    
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
    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)> _meListeningToOthers = [];
    
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
    {
        _othersListeningToMe.RemoveAll(wr => !wr.TryGetTarget(out _));
        _meListeningToOthers.RemoveAll(( tuple ) =>
        {
            (_, RefCheckingMethod refCheckingMethod, _) = tuple;
            return !refCheckingMethod(out _);
        });
    }
    
    private delegate bool RefCheckingMethod(out object target);
    public void BindFrom<TOther>(IProperty<TOther> other, Func<TOther, T> transformation)
    {
        Action<TOther> lambda = otherValue => Value = transformation(otherValue); 
        other.Connect(lambda);
        WeakReference<IProperty<TOther>> weakReferenceToOther = new(other);
        RefCheckingMethod refCheckingMethod = 
            (out target) =>
            {
                bool result = weakReferenceToOther.TryGetTarget(out IProperty<TOther> t);
                target = t;
                return result;
            };
        
        this._meListeningToOthers.Add( (  weakReferenceToOther, refCheckingMethod, lambda )  );
        this.Value = transformation(other.Value);
    }
    public void UnbindFrom<TOther>(IProperty<TOther> other, Func<TOther, T> transformation)
    {
        this._meListeningToOthers.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out var target);
            return target == lambda || target == null;
        });
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

    public void UnbindFrom<TOther>(IProperty<TOther> other, Func<TOther, T> transformation) 
        => _editableProperty.UnbindFrom(other, transformation);
}