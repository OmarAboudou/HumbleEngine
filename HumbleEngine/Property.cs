namespace HumbleEngine;

public interface IPropertyListener<T> : IDisposable
{
    public T Value { get; }

    public void Connect(Action<T> callback);
    public void Disconnect(Action<T> callback);

    public void BindFrom<TOther>(
        IPropertyListener<TOther> other,
        Func<TOther, T> transformation);

    public void UnbindFrom<TOther>(
        IPropertyListener<TOther> other,
        Func<TOther, T> transformation);
}

public sealed class Property<T> : IPropertyListener<T>
{
    internal Property(T initialValue)
    {
        _value = initialValue;
    }

    public static implicit operator T(Property<T> property) 
        => property.Value;

    public IPropertyListener<T> Listener => field ??= new PropertyListener<T>(this);
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

    private delegate bool RefCheckingMethod(out object target);

    private readonly List<WeakReference<Action<T>>> _othersListeningToMe = [];

    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)>
        _meListeningToOthers = [];

    public void Connect(Action<T> callback)
    {
        CleanConnections();
        if (_othersListeningToMe.Any(wr => wr.TryGetTarget(out Action<T>? target) && target == callback)
           )
        {
            Console.WriteLine(
                $"Callback {callback} is already connected to property {this} and cannot be connected multiple times.");
            return;
        }

        _othersListeningToMe.Add(new WeakReference<Action<T>>(callback));
    }

    public void Disconnect(Action<T> callback)
    {
        CleanConnections();

        _othersListeningToMe.RemoveAll(wr =>
        {
            wr.TryGetTarget(out Action<T>? target);
            return target == callback || target == null;
        });
    }

    private void Emit(T value)
    {
        CleanConnections();

        _othersListeningToMe.ForEach(c =>
        {
            if (c.TryGetTarget(out Action<T>? target))
                target?.Invoke(value);
        });
    }

    private void CleanConnections()
    {
        _othersListeningToMe.RemoveAll(wr => !wr.TryGetTarget(out _));
        _meListeningToOthers.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, _) = tuple;
            return !refCheckingMethod(out _);
        });
    }

    public void BindFrom<TOther>(
        IPropertyListener<TOther> other,
        Func<TOther, T> transformation)
    {
        Action<TOther> lambda = otherValue => Value = transformation(otherValue);
        other.Connect(lambda);
        WeakReference<IPropertyListener<TOther>> weakReferenceToOther = new(other);
        RefCheckingMethod refCheckingMethod =
            (out target) =>
            {
                bool result = weakReferenceToOther.TryGetTarget(out IPropertyListener<TOther> t);
                target = t;
                return result;
            };

        _meListeningToOthers.Add((weakReferenceToOther, refCheckingMethod, lambda));
        Value = transformation(other.Value);
    }

    public void UnbindFrom<TOther>(
        IPropertyListener<TOther> other,
        Func<TOther, T> transformation)
    {
        _meListeningToOthers.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out object? target);
            return target == lambda || target == null;
        });
    }

    public void BindTo<TOther>(
        Property<TOther> other,
        Func<T, TOther> transformation)
    {
        other.BindFrom(this, transformation);
    }

    public void UnbindTo<TOther>(
        Property<TOther> other,
        Func<T, TOther> transformation)
    {
        other.UnbindFrom(this, transformation);
    }

    public void BindFrom(IPropertyListener<T> other)
    {
        BindFrom(other, IdentityFunction);
    }

    public void UnbindFrom(IPropertyListener<T> other)
    {
        UnbindFrom(other, IdentityFunction);
    }

    public void BindTo(Property<T> other)
    {
        BindTo(other, IdentityFunction);
    }

    public void UnbindTo(Property<T> other)
    {
        UnbindTo(other, IdentityFunction);
    }

    public void Bind2WayFrom(Property<T> other)
    {
        Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    }

    public void Unbind2WayFrom(Property<T> other)
    {
        Unbind2WayFrom(other, IdentityFunction, IdentityFunction);
    }

    public void Bind2WayTo(Property<T> other)
    {
        other.Bind2WayFrom(this);
    }

    public void Unbind2WayTo(Property<T> other)
    {
        other.Unbind2WayFrom(this);
    }

    public void Bind2WayFrom<TOther>(
        Property<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        BindFrom(other, transformationFrom);
        other.BindFrom(this, transformationTo);
    }

    public void Unbind2WayFrom<TOther>(
        Property<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(this, transformationTo);
        UnbindFrom(other, transformationFrom);
    }

    public void Bind2WayTo<TOther>(
        Property<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.Bind2WayFrom(this, transformationTo, transformationFrom);
    }

    public void Unbind2WayTo<TOther>(
        Property<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.Unbind2WayFrom(this, transformationTo, transformationFrom);
    }

    public void Dispose()
    {
        _othersListeningToMe.Clear();
        _meListeningToOthers.Clear();
    }
}

public sealed class PropertyListener<T> : IPropertyListener<T>
{
    internal PropertyListener(Property<T> property)
    {
        _property = property;
    }

    public static implicit operator T(PropertyListener<T> propertyListener)
    {
        return propertyListener.Value;
    }

    private readonly Property<T> _property;

    public T Value => _property.Value;

    public void Connect(Action<T> callback)
    {
        _property.Connect(callback);
    }

    public void Disconnect(Action<T> callback)
    {
        _property.Disconnect(callback);
    }

    public void BindFrom<TDest>(IPropertyListener<TDest> other, Func<TDest, T> transformation)
    {
        _property.BindFrom(other, transformation);
    }

    public void UnbindFrom<TOther>(IPropertyListener<TOther> other, Func<TOther, T> transformation)
    {
        _property.UnbindFrom(other, transformation);
    }

    public void Dispose()
    {
    }
}

public static class PropertyExtensions
{
    /*public static void BindTo<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.BindFrom(This, transformation);
    public static void UnbindTo<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.UnbindFrom(This, transformation);

    public static void BindFrom<T>(
        this IProperty<T> This,IProperty<T> other)
        => This.BindFrom(other, IdentityFunction);

    public static void UnbindFrom<T>(
        this IProperty<T> This, IProperty<T> other)
        => This.UnbindFrom(other, IdentityFunction);

    public static void BindTo<T>(
        this IProperty<T> This,IProperty<T> other)
        => This.BindTo(other, IdentityFunction);
    public static void UnbindTo<T>(
        this IProperty<T> This,IProperty<T> other)
        => This.UnbindTo(other, IdentityFunction);

    public static void Bind2WayFrom<T>(
        this IProperty<T> This,IProperty<T> other)
        => This.Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public static void Unbind2WayFrom<T>(
        this IProperty<T> This,IProperty<T> other)
        => This.Unbind2WayFrom(other, IdentityFunction, IdentityFunction);

    public static void Bind2WayTo<T>(
        this IProperty<T> This,IProperty<T> other)
        => other.Bind2WayFrom(This);
    public static void Unbind2WayTo<T>(
        this IProperty<T> This,IProperty<T> other)
        => other.Unbind2WayFrom(This);

    public static void Bind2WayFrom<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        This.BindFrom(other, transformationFrom);
        other.BindFrom(This, transformationTo);
    }
    public static void Unbind2WayFrom<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(This, transformationTo);
        This.UnbindFrom(other, transformationFrom);
    }

    public static void Bind2WayTo<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Bind2WayFrom(This, transformationTo, transformationFrom);
    public static void Unbind2WayTo<TOther, T>(
        this IProperty<T> This,
        IProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(This, transformationTo, transformationFrom);*/
}